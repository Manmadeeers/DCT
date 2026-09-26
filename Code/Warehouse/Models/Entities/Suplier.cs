namespace Warehouse.Models.Entities
{
    public sealed class Suplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}