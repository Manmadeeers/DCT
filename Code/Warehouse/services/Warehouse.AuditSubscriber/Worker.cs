using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Messaging;

namespace Warehouse.AuditSubscriber;


//INFO: In order to switch back to RabbitMQ  change <BrokerOptions> to <RabbitmqOptions> and :BrokerSubscriber to :RabbitmqSubscriber
public sealed class Worker(
    IOptions<BrokerOptions> options,
    ILogger<Worker> logger)
    : BrokerSubscriber(options, logger)
{
    protected override string QueueName =>
        RabbitTopology.AuditQueue;

    protected override Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct)
    {
        logger.LogInformation(
            "AUDIT EventId={EventId} Type={EventType} OccurredAt={OccurredAt} Data={Data}",
            message.EventId,
            message.EventType,
            message.OccurredAtUtc,
            message.Data.GetRawText());

        return Task.CompletedTask;
    }
}