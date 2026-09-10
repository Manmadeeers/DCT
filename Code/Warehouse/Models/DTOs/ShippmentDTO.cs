using Warehouse.Models.Entities;

namespace Warehouse.Models.DTOs
{
    public class ShippmentDTO
    {
        public int Id { get; set; }
        public string Shipment_number { get; set; } = string.Empty;
        public int Supplier_id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int Warehouse_id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime Expected_date { get; set; }
        public DateTime? Received_date { get; set; }
        public ShippmentStatus Shipment_status { get; set; }
        public string Tracking_number { get; set; } = string.Empty;
        public decimal Shipping_fee { get; set; }
        public string? Notes { get; set; }
        public List<ShippingItemDTO> ShippingItems { get; set; } = new();
    }

    public class CreateShippmentRequest
    {
        public int Supplier_id { get; set; }
        public int Warehouse_id { get; set; }
        public DateTime Expected_date { get; set; }
        public string Tracking_number { get; set; } = string.Empty;
        public decimal Shipping_fee { get; set; }
        public string? Notes { get; set; }
        public List<CreateShippingItemRequest> ShippingItems { get; set; } = new();
    }

    public class ReceiveShippmentRequest
    {
        public List<ReceiveItemRequest> Items { get; set; } = new();
    }
}