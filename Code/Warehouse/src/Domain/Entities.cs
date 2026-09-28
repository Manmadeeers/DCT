namespace Warehouse.Api.Domain;

public sealed class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

public sealed class Stock
{
    public int Id { get; set; }
    public int? WarehouseId { get; set; }
    public string Name { get; set; } = null!;
    public Warehouse? Warehouse { get; set; }
    public ICollection<StockItem> Items { get; set; } = new List<StockItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();
}

public sealed class Suplier
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

public sealed class StockItem
{
    public int StockId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Reserved { get; set; }
    public int Available => Quantity - Reserved;
    public Stock Stock { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class Shipment
{
    public int Id { get; set; }
    public int? WarehouseId { get; set; }
    public int? StockId { get; set; }
    public int? SupplierId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Stock? Stock { get; set; }
    public Suplier? Supplier { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
}

public sealed class ShipmentItem
{
    public int ShipmentId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
