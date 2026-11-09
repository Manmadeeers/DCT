using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Messaging;

namespace Warehouse.Api.Infrastructure;

public sealed class BrokerPublisherWorker(
    WarehouseEventBuffer buffer,
    IOptions<BrokerOptions> options,
    ILogger<BrokerPublisherWorker> logger)
    : BackgroundService
{
    private readonly BrokerClient _client = new(options);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await foreach (
                var pending in buffer.Reader.ReadAllAsync(stoppingToken))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _client.PublishAsync(
                            pending.RoutingKey,
                            pending.Envelope,
                            stoppingToken);

                        break;
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(
                            exception,
                            "Publishing failed. Retrying EventId={EventId}",
                            pending.Envelope.EventId);

                        await Task.Delay(
                            TimeSpan.FromSeconds(2),
                            stoppingToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    public override void Dispose()
    {
        _client.Dispose();
        base.Dispose();
    }
}