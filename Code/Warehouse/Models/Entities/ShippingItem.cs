namespace Warehouse.Models.Entities
{
    public class ShippingItem
    {
        public int Id { get; set; }
        public int Shipment_id { get; set; }
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_cost { get; set; }

        public Shippment Shippment { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}