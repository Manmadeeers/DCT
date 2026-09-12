namespace Warehouse.Models.DTOs
{
    public class StockDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockUnits { get; set; }
        public int ReservedUnits { get; set; }
        public int AvailableUnits { get; set; }
    }

    public class CreateStockRequest
    {
        public string Name { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public int StockUnits { get; set; }
    }

    public class UpdateStockRequest
    {
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public int? StockUnits { get; set; }
        public int? ReservedUnits { get; set; }
    }

    public class ReserveProductRequest
    {
        public int Product_id { get; set; }
        public int Quantity { get; set; }
    }
}