using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Models.Entities
{
    public class Stock
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int StockUnits { get; set; }
        public int ReservedUnits { get; set; }
        [NotMapped]
        public int AvailableUnits => StockUnits - ReservedUnits;

        public int? WarehouseId{get;set;}
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public Warehouse Warehouse { get; set; } = null!;
    }
}