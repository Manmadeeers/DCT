using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Domain;

namespace Warehouse.Api.Data;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse.Api.Domain.Warehouse> Warehouses => Set<Warehouse.Api.Domain.Warehouse>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Suplier> Supliers => Set<Suplier>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var warehouse = modelBuilder.Entity<Warehouse.Api.Domain.Warehouse>();
        warehouse.ToTable("warehouse");
        warehouse.HasKey(x => x.Id);
        warehouse.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        warehouse.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        warehouse.HasIndex(x => x.Name).IsUnique();

        var stock = modelBuilder.Entity<Stock>();
        stock.ToTable("stock");
        stock.HasKey(x => x.Id);
        stock.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        stock.Property(x => x.WarehouseId).HasColumnName("warehouseid");
        stock.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        stock.HasOne(x => x.Warehouse).WithMany(x => x.Stocks)
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);

        var product = modelBuilder.Entity<Product>();
        product.ToTable("product");
        product.HasKey(x => x.Id);
        product.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        product.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        product.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        product.HasIndex(x => x.Sku).IsUnique();

        var suplier = modelBuilder.Entity<Suplier>();
        suplier.ToTable("suplier");
        suplier.HasKey(x => x.Id);
        suplier.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        suplier.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        suplier.HasIndex(x => x.Name).IsUnique();

        var stockItem = modelBuilder.Entity<StockItem>();
        stockItem.ToTable("stockitem");
        // Logical EF key. Your SQL does not enforce it; see db/optional-constraints.sql.
        stockItem.HasKey(x => new { x.StockId, x.ProductId });
        stockItem.Property(x => x.StockId).HasColumnName("stockid");
        stockItem.Property(x => x.ProductId).HasColumnName("productid");
        stockItem.Property(x => x.Quantity).HasColumnName("quantity");
        stockItem.Property(x => x.Reserved).HasColumnName("reserved");
        stockItem.Ignore(x => x.Available);
        stockItem.HasOne(x => x.Stock).WithMany(x => x.Items)
            .HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.NoAction);
        stockItem.HasOne(x => x.Product).WithMany(x => x.StockItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);

        var shipment = modelBuilder.Entity<Shipment>();
        shipment.ToTable("shipment");
        shipment.HasKey(x => x.Id);
        shipment.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        shipment.Property(x => x.WarehouseId).HasColumnName("warehouseid");
        shipment.Property(x => x.StockId).HasColumnName("stockid");
        shipment.Property(x => x.SupplierId).HasColumnName("suplierid");
        shipment.HasOne(x => x.Warehouse).WithMany(x => x.Shipments)
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        shipment.HasOne(x => x.Stock).WithMany(x => x.Shipments)
            .HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.NoAction);
        shipment.HasOne(x => x.Supplier).WithMany(x => x.Shipments)
            .HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.NoAction);

        var shipmentItem = modelBuilder.Entity<ShipmentItem>();
        shipmentItem.ToTable("shipmentitem");
        // Logical EF key. Your SQL does not enforce it; see db/optional-constraints.sql.
        shipmentItem.HasKey(x => new { x.ShipmentId, x.ProductId });
        shipmentItem.Property(x => x.ShipmentId).HasColumnName("shipmentid");
        shipmentItem.Property(x => x.ProductId).HasColumnName("productid");
        shipmentItem.Property(x => x.Quantity).HasColumnName("quanitty");
        shipmentItem.HasOne(x => x.Shipment).WithMany(x => x.Items)
            .HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
        shipmentItem.HasOne(x => x.Product).WithMany(x => x.ShipmentItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
    }
}
