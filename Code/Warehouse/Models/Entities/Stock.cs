namespace Warehouse.Models.Entities
{
    public class Stock
    {
        public int Id { get; set; }
        public string? Name{get;set;}
        public int Product_id { get; set; }
        public int Warehouse_id { get; set; }
        public int Quantity { get; set; }
        public int Reserved_quantity { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        public int AvailableQuantity => Quantity - Reserved_quantity;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public Warehouse Warehouse { get; set; } = null!;
    }
}