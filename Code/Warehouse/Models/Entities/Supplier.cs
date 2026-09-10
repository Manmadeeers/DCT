namespace Warehouse.Models.Entities
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        public ICollection<Shippment>Shippments{get;set;} = new List<Shippment>();
    }
}