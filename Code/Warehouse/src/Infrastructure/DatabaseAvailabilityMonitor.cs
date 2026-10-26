using Npgsql;

namespace Warehouse.Api.Infrastructure;

public sealed record DatabaseNodeStatus(
    string Name,
    bool Reachable,
    string Role,
    bool IsWriter,
    bool HasSynchronousStandby,
    bool ReceivingWal,
    string? Error);

public sealed record DatabaseClusterStatus(
    DateTimeOffset CheckedAt,
    bool Ready,
    bool SplitBrainSuspected,
    string? ActiveWriter,
    DatabaseNodeStatus[] Nodes);

public sealed class DatabaseAvailabilityMonitor : BackgroundService
{
    private readonly ILogger<DatabaseAvailabilityMonitor> _logger;

    private readonly (string Name, string ConnectionString)[] _nodes;

    private DatabaseClusterStatus _current = new(
        DateTimeOffset.MinValue,
        false,
        false,
        null,
        []);

    public DatabaseAvailabilityMonitor(
        IConfiguration configuration,
        ILogger<DatabaseAvailabilityMonitor> logger)
    {
        _logger = logger;

        _nodes =
        [
            (
                "dct",
                configuration.GetConnectionString("PrimaryNode")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:PrimaryNode is required.")
            ),
            (
                "dct-replica",
                configuration.GetConnectionString("ReplicaNode")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:ReplicaNode is required.")
            )
        ];
    }

    public DatabaseClusterStatus Current =>
        Volatile.Read(ref _current);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nodes = await Task.WhenAll(
                _nodes.Select(node => ProbeAsync(
                    node.Name,
                    node.ConnectionString,
                    stoppingToken)));

            var writers = nodes
                .Where(node => node.IsWriter)
                .ToArray();

            var writer = writers.Length == 1
                ? writers[0]
                : null;

            var next = new DatabaseClusterStatus(
                DateTimeOffset.UtcNow,
                Ready: writer is not null &&
                       writer.HasSynchronousStandby,
                SplitBrainSuspected: writers.Length > 1,
                ActiveWriter: writer?.Name,
                Nodes: nodes);

            var previous = Current;

            Volatile.Write(ref _current, next);

            if (!previous.Nodes.SequenceEqual(next.Nodes))
            {
                _logger.LogInformation(
                    "Database cluster changed. Writer={Writer}, " +
                    "Ready={Ready}, SplitBrainSuspected={SplitBrain}",
                    next.ActiveWriter ?? "none",
                    next.Ready,
                    next.SplitBrainSuspected);

                foreach (var node in nodes)
                {
                    _logger.LogInformation(
                        "{Node}: Reachable={Reachable}, Role={Role}, " +
                        "SynchronousStandby={SynchronousStandby}, " +
                        "ReceivingWal={ReceivingWal}, Error={Error}",
                        node.Name,
                        node.Reachable,
                        node.Role,
                        node.HasSynchronousStandby,
                        node.ReceivingWal,
                        node.Error);
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }

    private static async Task<DatabaseNodeStatus> ProbeAsync(
        string name,
        string connectionString,
        CancellationToken stoppingToken)
    {
        try
        {
            using var timeout = CancellationTokenSource
                .CreateLinkedTokenSource(stoppingToken);

            timeout.CancelAfter(TimeSpan.FromSeconds(3));

            var options =
                new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Pooling = false,
                    Timeout = 2,
                    CommandTimeout = 2
                };

            await using var connection =
                new NpgsqlConnection(options.ConnectionString);

            await connection.OpenAsync(timeout.Token);

            const string sql = """
                SELECT
                    pg_is_in_recovery(),
                    current_setting('transaction_read_only')::boolean,
                    EXISTS (
                        SELECT 1
                        FROM pg_stat_replication
                        WHERE state = 'streaming'
                          AND sync_state IN ('sync', 'quorum')
                    ),
                    EXISTS (
                        SELECT 1
                        FROM pg_stat_wal_receiver
                        WHERE status = 'streaming'
                    );
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync(timeout.Token);

            await reader.ReadAsync(timeout.Token);

            var inRecovery = reader.GetBoolean(0);
            var readOnly = reader.GetBoolean(1);

            return new DatabaseNodeStatus(
                name,
                Reachable: true,
                Role: inRecovery ? "Replica" : "Primary",
                IsWriter: !inRecovery && !readOnly,
                HasSynchronousStandby: reader.GetBoolean(2),
                ReceivingWal: reader.GetBoolean(3),
                Error: null);
        }
        catch (Exception exception)
            when (!stoppingToken.IsCancellationRequested)
        {
            return new DatabaseNodeStatus(
                name,
                Reachable: false,
                Role: "Unknown",
                IsWriter: false,
                HasSynchronousStandby: false,
                ReceivingWal: false,
                Error: exception is OperationCanceledException
                    ? "Probe timed out."
                    : exception.GetType().Name);
        }
    }
}