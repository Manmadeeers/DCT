namespace Warehouse.Models.DTOs
{
    public class ShippingItemDTO
    {
        public int Id { get; set; }
        public int Shipment_id { get; set; }
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_cost { get; set; }

    }

    public class CreateShippingItemRequest
    {
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_cost { get; set; }

    }

    public class ReceiveItemRequest
    {
        public int Product_id { get; set; }
        public int Quantity { get; set; }
    }

    public class ShipProductRequest
    {
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public string Shipment_number { get; set; } = string.Empty;
        public string? Destination { get; set; }
    }
}