using RabbitMQ.Client;

namespace Warehouse.Messaging;

public static class RabbitTopology
{
    public const string Exchange = "warehouse.events";

    public const string AuditQueue = "warehouse.audit";
    public const string NotificationQueue = "warehouse.notifications";
    public const string CatalogQueue = "warehouse.catalog";
    public const string AnalyticsQueue = "warehouse.analytics";

    public const string DeadLetterExchange = "warehouse.deadletter";
    public const string DeadLetterQueue = "warehouse.deadletter";

    public const string StockUpdatedKey = "warehouse.stock.updated";
    public const string ProductAddedKey = "warehouse.product.added";

    public static async Task DeclareAsync(
        IChannel channel,
        CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await DeclareQueueAsync(
            channel,
            DeadLetterQueue,
            useDeadLettering: false,
            ct: ct);

        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: "failed",
            cancellationToken: ct);

        await DeclareQueueAsync(channel, AuditQueue, true, ct);
        await DeclareQueueAsync(channel, NotificationQueue, true, ct);
        await DeclareQueueAsync(channel, CatalogQueue, true, ct);
        await DeclareQueueAsync(channel, AnalyticsQueue, true, ct);

        await channel.QueueBindAsync(
            queue: AuditQueue,
            exchange: Exchange,
            routingKey: "warehouse.#",
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: NotificationQueue,
            exchange: Exchange,
            routingKey: StockUpdatedKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: CatalogQueue,
            exchange: Exchange,
            routingKey: ProductAddedKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: AnalyticsQueue,
            exchange: Exchange,
            routingKey: "warehouse.#",
            cancellationToken: ct);
    }

    private static async Task DeclareQueueAsync(
        IChannel channel,
        string queueName,
        bool useDeadLettering,
        CancellationToken ct)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-quorum-initial-group-size"] = 3
        };

        if (useDeadLettering)
        {
            arguments["x-dead-letter-exchange"] = DeadLetterExchange;
            arguments["x-dead-letter-routing-key"] = "failed";

            arguments["x-dead-letter-strategy"] = "at-least-once";
            arguments["x-overflow"] = "reject-publish";
        }

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: ct);
    }
}