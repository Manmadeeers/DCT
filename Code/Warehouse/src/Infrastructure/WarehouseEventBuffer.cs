using System.Text.Json;
using System.Threading.Channels;
using Warehouse.Messaging;

namespace Warehouse.Api.Infrastructure;

public sealed record PendingEvent(
    string RoutingKey,
    EventEnvelope Envelope);

public sealed class WarehouseEventBuffer
{
    private readonly Channel<PendingEvent> _channel =
        Channel.CreateBounded<PendingEvent>(
            new BoundedChannelOptions(10_000)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });

    public ChannelReader<PendingEvent> Reader => _channel.Reader;

    public bool TryPublish(object domainEvent)
    {
        var eventType = domainEvent.GetType();

        var routingKey = eventType.Name switch
        {
            "ProductAdded" => RabbitTopology.ProductAddedKey,
            "ShipmentReceived" => "warehouse.shipment.received",
            "ProductReserved" => "warehouse.product.reserved",
            "ProductShipped" => "warehouse.product.shipped",
            "StockUpdated" => RabbitTopology.StockUpdatedKey,

            _ => $"warehouse.other.{eventType.Name.ToLowerInvariant()}"
        };

        var data = JsonSerializer.SerializeToElement(
            domainEvent,
            eventType,
            EventJson.Options);

        var envelope = new EventEnvelope(
            EventId: Guid.NewGuid(),
            EventType: eventType.Name,
            Version: 1,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Data: data);

        return _channel.Writer.TryWrite(
            new PendingEvent(routingKey, envelope));
    }
}