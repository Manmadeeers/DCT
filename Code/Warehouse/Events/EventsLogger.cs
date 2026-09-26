namespace Warehouse.Events
{
    public sealed class WarehouseEventLogger(ILogger<WarehouseEventLogger> logger)
    {
        public void Publish(IWarehouseEvent warehouseEvent)
        {
            logger.LogInformation(
                "Warehouse event {EventType}: {Event}",
                warehouseEvent.GetType().Name,
                warehouseEvent
            );
        }
    }
}