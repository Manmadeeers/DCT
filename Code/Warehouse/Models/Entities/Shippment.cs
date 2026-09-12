namespace Warehouse.Models.Entities
{

    public enum ShippmentStatus
    {
        Created,
        InTransit,
        Delivered,
        Received,
        Canceled
    }
    public class Shippment
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public DateTime ExpectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public ShippmentStatus ShipmentStatus { get; set; } = ShippmentStatus.Created;
        public decimal ShippingFee { get; set; }
        public int? SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public int? WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
        public ICollection<ShippingItem> ShippingItems { get; set; } = new List<ShippingItem>();
    }
}