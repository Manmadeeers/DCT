namespace Warehouse.Models.Entities
{
    public sealed class Product
    {
        public int Id { get; set; }
        public string Sku { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
        public ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();
    }
}