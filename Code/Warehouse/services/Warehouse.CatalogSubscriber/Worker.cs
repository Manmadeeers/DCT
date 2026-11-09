using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Messaging;

namespace Warehouse.CatalogSubscriber;

//INFO: In order to switch back to RabbitMQ change <BrokerOptions> to <RabbitmqOptions> and :BrokerSubscriber to :RabbitmqSubscriber
public sealed class Worker(
    IOptions<BrokerOptions> options,
    ILogger<Worker> logger)
    : BrokerSubscriber(options, logger)
{
    protected override string QueueName =>
        RabbitTopology.CatalogQueue;

    protected override Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct)
    {
        if (message.EventType != "ProductAdded")
        {
            throw new InvalidOperationException(
                $"Unexpected event: {message.EventType}");
        }

        var productId = message.Data
            .GetProperty("productId")
            .GetInt32();

        var name = message.Data
            .GetProperty("name")
            .GetString()
            ?? throw new JsonException(
                "ProductAdded must contain a product name.");

        logger.LogInformation(
            "CATALOG ProductAdded EventId={EventId} ProductId={ProductId} Name={Name}",
            message.EventId,
            productId,
            name);

        return Task.CompletedTask;
    }
}