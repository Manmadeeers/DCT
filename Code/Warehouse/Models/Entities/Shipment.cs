namespace Warehouse.Models.Entities
{

    public sealed class Shipment
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int StockId { get; set; }
        public int SuplierId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
        public Stock Stock { get; set; } = null!;
        public Suplier Suplier { get; set; } = null!;
        public ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();
    }
}