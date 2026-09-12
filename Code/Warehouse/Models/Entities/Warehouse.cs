namespace Warehouse.Models.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string City { get; set; } = null!;

        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public ICollection<Shippment>Shippments{get;set;} = new List<Shippment>();
    }
}