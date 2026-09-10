namespace Warehouse.Models.DTOs
{
    public class StockDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Warehouse_id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Reserved_quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int ProductCount { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }

    public class CreateStockRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Warehouse_id { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateStockRequest
    {
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public int? Reserved_quantity { get; set; }
    }

    public class ReserveProductRequest
    {
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public string? Reference { get; set; }
    }
}