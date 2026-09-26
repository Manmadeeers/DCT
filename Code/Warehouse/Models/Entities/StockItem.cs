using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Models.Entities
{
    public sealed class StockItem
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Reserved { get; set; }
        [NotMapped]
        public int Available => Quantity - Reserved;
        public Stock Stock { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}