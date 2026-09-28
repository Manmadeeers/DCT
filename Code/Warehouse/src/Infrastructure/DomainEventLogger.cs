using Warehouse.Api.Domain;

namespace Warehouse.Api.Infrastructure;

public sealed class DomainEventLogger(ILogger<DomainEventLogger> logger)
{
    public void Publish(IDomainEvent domainEvent) => logger.LogInformation(
        "Warehouse event {EventType}: {Event}", domainEvent.GetType().Name, domainEvent);
}
