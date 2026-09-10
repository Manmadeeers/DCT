namespace Warehouse.Models.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        public ICollection<Stock> Stocks{get;set;} = new List<Stock>();
        public ICollection<Shippment> Shippments{get;set;} = new List<Shippment>();
    }
}