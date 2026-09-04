namespace Warehouse.Models
{

    public enum ShipmentStatus
    {
        Created,
        InTransit,
        Delivered,
        Received,
        Canceled
    }
    public class Shipment
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; }
        public Suplier Supplier { get; set; }
        public Warehouse Warehouse { get; set; }
        public DateTime ExpectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public ShipmentStatus Status { get; set; }
        public string TrackingNumber { get; set; }
        public decimal ShippingFee { get; set; }
        public string? Notes { get; set; }
        public List<ShipmentItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }

    public class ShipmentItem
    {
        public int Id { get; set; }
        public Shipment Shipment { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}