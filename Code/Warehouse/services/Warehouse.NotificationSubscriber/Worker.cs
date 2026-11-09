using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Messaging;

namespace Warehouse.NotificationSubscriber;

public sealed class Worker(
    IOptions<BrokerOptions> options,
    IConfiguration configuration,
    ILogger<Worker> logger)
    : BrokerSubscriber(options, logger)
{
    private readonly int _lowStockThreshold =
        configuration.GetValue<int>(
            "Notifications:LowStockThreshold",
            5);

    protected override string QueueName =>
        RabbitTopology.NotificationQueue;

    protected override Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct)
    {
        if (message.EventType != "StockUpdated")
        {
            throw new InvalidOperationException(
                $"Unexpected event: {message.EventType}");
        }

        var stockId = message.Data
            .GetProperty("stockId")
            .GetInt32();

        var productId = message.Data
            .GetProperty("productId")
            .GetInt32();

        var available = message.Data
            .GetProperty("available")
            .GetInt32();

        logger.LogInformation(
            "STOCK CHECK EventId={EventId} Stock={StockId} Product={ProductId} Available={Available}",
            message.EventId,
            stockId,
            productId,
            available);

        if (available <= _lowStockThreshold)
        {
            logger.LogWarning(
                "LOW STOCK Stock={StockId} Product={ProductId} Available={Available} Threshold={Threshold}",
                stockId,
                productId,
                available,
                _lowStockThreshold);
        }

        return Task.CompletedTask;
    }
}
