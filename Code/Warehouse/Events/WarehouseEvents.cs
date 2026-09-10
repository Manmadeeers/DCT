using Warehouse.Models;

namespace Warehouse.Events
{
    public interface IWarehouseEvent
    {
        DateTime OccuredAt { get; }
    }

    public class ShipmentReceivedEvent : IWarehouseEvent
    {
        public int ShipmentId { get; set; }
        public int WarehouseId { get; set; }

        public List<ShipmentItem> Items { get; set; }
        public DateTime OccuredAt { get; set; }
    }

    public class ProductReservedEvent : IWarehouseEvent
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public DateTime OccuredAt { get; set; }
    }

    public class ProductShippedEvent : IWarehouseEvent
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public string ShipmentDestination { get; set; }
        public DateTime OccuredAt { get; set; }
    }

    public class StockUpdatedEvent : IWarehouseEvent
    {
        public int StockId { get; set; }
        public int WarehouseId { get; set; }
        public int ProductId { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string OperationType { get; set; }
        public DateTime OccuredAt { get; set; }
    }
}