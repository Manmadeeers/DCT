using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Models.Entities
{
    public sealed class Stock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public string Name { get; set; } = String.Empty;

        public Warehouse Warehouse { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    }
}