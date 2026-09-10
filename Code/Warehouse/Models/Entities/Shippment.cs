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
        public string Shipment_number { get; set; } = string.Empty;
        public int Supplier_id { get; set; }
        public int Warehouse_id { get; set; }
        public DateTime Expected_date { get; set; }
        public DateTime? Received_date { get; set; }
        public ShippmentStatus Shipment_status { get; set; } = ShippmentStatus.Created;
        public string Tracking_number { get; set; } = string.Empty;
        public decimal Shipping_fee { get; set; }
        public string? Notes { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public ICollection<ShippingItem> ShippingItems { get; set; } = new List<ShippingItem>();
    }
}