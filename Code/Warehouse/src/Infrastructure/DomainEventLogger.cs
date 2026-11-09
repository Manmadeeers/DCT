namespace Warehouse.Api.Infrastructure;

public sealed class DomainEventLogger(
    ILogger<DomainEventLogger> logger,
    WarehouseEventBuffer? publishBuffer = null)
{
    public void Publish<T>(T domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        logger.LogInformation(
            "Domain event {EventType}: {Event}",
            domainEvent.GetType().Name,
            domainEvent);

        if (publishBuffer is null)
        {
            return;
        }

        if (!publishBuffer.TryPublish(domainEvent))
        {
            logger.LogError(
                "Publishing buffer is full. Event {EventType} was dropped.",
                domainEvent.GetType().Name);
        }
    }
}