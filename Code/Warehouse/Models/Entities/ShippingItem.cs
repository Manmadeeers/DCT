namespace Warehouse.Models.Entities
{
    public class ShippingItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public int? ShippmentId { get; set; }
        public Shippment Shippment { get; set; } = null!;

        public int? ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}