namespace Warehouse.Models
{
    public class Stock
    {
        public int Id { get; set; }
        public Product Product { get; set; }
        public Warehouse Warehouse { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }

    }
}