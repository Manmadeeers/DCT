using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Warehouse.Messaging;

public abstract class BrokerSubscriber : BackgroundService
{
    private readonly BrokerClient _client;
    private readonly ILogger _logger;

    protected BrokerSubscriber(
        IOptions<BrokerOptions> options,
        ILogger logger)
    {
        _client = new BrokerClient(options);
        _logger = logger;
    }

    protected abstract string QueueName { get; }

    protected abstract Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Subscriber is listening to {Queue}",
            QueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delivery = await _client.ReceiveAsync(
                    QueueName,
                    stoppingToken);

                if (delivery is null)
                {
                    continue;
                }

                try
                {
                    await HandleAsync(
                        delivery.Envelope,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Processing failed. Queue={Queue} EventId={EventId} " +
                        "Attempt={Attempt}",
                        QueueName,
                        delivery.Envelope.EventId,
                        delivery.Attempt);

                    var rejected = await _client.RejectAsync(
                        QueueName,
                        delivery.Receipt,
                        exception.Message,
                        stoppingToken);

                    if (!rejected)
                    {
                        _logger.LogWarning(
                            "Rejection receipt expired. EventId={EventId}",
                            delivery.Envelope.EventId);
                    }

                    continue;
                }

                var acknowledged = await _client.AckAsync(
                    QueueName,
                    delivery.Receipt,
                    stoppingToken);

                if (!acknowledged)
                {
                    _logger.LogWarning(
                        "Acknowledgement receipt expired. " +
                        "Event may be delivered again. EventId={EventId}",
                        delivery.Envelope.EventId);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Broker communication failed. Queue={Queue}. Retrying.",
                    QueueName);

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _client.Dispose();
        base.Dispose();
    }
}