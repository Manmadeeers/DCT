namespace Warehouse.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
        public int? Stock_id { get; set; }

        public Stock? Stock { get; set; }
        public ICollection<ShippingItem> ShippingItem { get; set; } = new List<ShippingItem>();
    }
}