using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Messaging;

namespace Warehouse.EventAnalyticsSubscriber;

//INFO: In order to switch back to RabbitMQ change <BrokerOptions> to <RabbitmqOptions> and :BrokerSubscriber to :RabbitmqSubscriber

public sealed class Worker : BrokerSubscriber
{
    private static readonly JsonSerializerOptions StateJson = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<Worker> _logger;
    private readonly string _stateFile;
    private readonly object _gate = new();

    private AnalyticsState _state;

    public Worker(
        IOptions<BrokerOptions> brokerOptions,
        IOptions<AnalyticsOptions> analyticsOptions,
        ILogger<Worker> logger)
        : base(brokerOptions, logger)
    {
        _logger = logger;

        _stateFile = Path.GetFullPath(
            analyticsOptions.Value.StateFile);

        Directory.CreateDirectory(
            Path.GetDirectoryName(_stateFile)!);

        _state = File.Exists(_stateFile)
            ? JsonSerializer.Deserialize<AnalyticsState>(
                  File.ReadAllText(_stateFile),
                  StateJson)
              ?? throw new InvalidDataException(
                  "Analytics state file contains no state.")
            : new AnalyticsState();

        _logger.LogInformation(
            "Analytics loaded. TotalEvents={TotalEvents}",
            _state.TotalEvents);
    }

    protected override string QueueName =>
        RabbitTopology.AnalyticsQueue;

    protected override Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_state.ProcessedEventIds.Contains(message.EventId))
            {
                _logger.LogInformation(
                    "ANALYTICS duplicate ignored EventId={EventId}",
                    message.EventId);

                return Task.CompletedTask;
            }

            var eventDate = message.OccurredAtUtc
                .UtcDateTime
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var next = new AnalyticsState
            {
                TotalEvents = _state.TotalEvents + 1,
                EventsByType = new Dictionary<string, long>(
                    _state.EventsByType),
                EventsByUtcDate = new Dictionary<string, long>(
                    _state.EventsByUtcDate),
                LastObservedAtUtc = DateTimeOffset.UtcNow,
                ProcessedEventIds = new HashSet<Guid>(
                    _state.ProcessedEventIds)
            };

            next.EventsByType[message.EventType] =
                next.EventsByType.GetValueOrDefault(message.EventType) + 1;

            next.EventsByUtcDate[eventDate] =
                next.EventsByUtcDate.GetValueOrDefault(eventDate) + 1;

            next.ProcessedEventIds.Add(message.EventId);

            // Save before returning: the base subscriber acknowledges afterward.
            SaveState(next);

            _state = next;

            _logger.LogInformation(
                "ANALYTICS EventType={EventType} " +
                "TypeCount={TypeCount} TotalEvents={TotalEvents} " +
                "EventUtcDate={EventUtcDate} DateCount={DateCount}",
                message.EventType,
                next.EventsByType[message.EventType],
                next.TotalEvents,
                eventDate,
                next.EventsByUtcDate[eventDate]);

            return Task.CompletedTask;
        }
    }

    private void SaveState(AnalyticsState state)
    {
        var temporaryFile = _stateFile + ".tmp";

        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            state,
            StateJson);

        using (var stream = new FileStream(
            temporaryFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        File.Move(
            temporaryFile,
            _stateFile,
            overwrite: true);
    }
}
