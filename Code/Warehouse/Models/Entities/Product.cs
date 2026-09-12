namespace Warehouse.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? StockId { get; set; }
        public Stock? Stock { get; set; }
        public ICollection<ShippingItem> ShippingItems { get; set; } = new List<ShippingItem>();
    }
}