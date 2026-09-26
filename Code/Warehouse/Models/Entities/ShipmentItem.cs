namespace Warehouse.Models.Entities
{
    public class ShipmentItem
    {
        public int ShipmentId { get; set; }
        public int ProducId { get; set; }
        public int Qunatity { get; set; }
        public Shipment Shipment { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}