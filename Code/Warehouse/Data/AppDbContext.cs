using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Warehouse.Models.Entities;

namespace Warehouse.Data
{
    public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
    {
        public DbSet<Models.Entities.Warehouse> Warehouses => Set<Models.Entities.Warehouse>();
        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<StockItem> StockItems => Set<StockItem>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Suplier> Supliers => Set<Suplier>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureWarehouse(modelBuilder);
            ConfigureStock(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureSuplier(modelBuilder);
            ConfigureStockItem(modelBuilder);
            ConfigureShipment(modelBuilder);
            ConfigureShipmentItem(modelBuilder);
        }

        private static void ConfigureWarehouse(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Models.Entities.Warehouse>();
            entity.ToTable("warehouse");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).HasMaxLength(50).IsRequired();
        }

        private static void ConfigureStock(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Stock>();
            entity.ToTable("stock");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(50).IsRequired();
        }

        private static void ConfigureProduct(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Product>();
            entity.ToTable("product");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(p => p.Sku).IsUnique();
        }

        private static void ConfigureSuplier(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Suplier>();
            entity.ToTable("suplier");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(s => s.Name).IsUnique();
        }

        private static void ConfigureStockItem(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<StockItem>();
            entity.ToTable("stockItem");
            entity.HasKey(s => new { s.StockId, s.ProductId });
            entity.Ignore(s => s.Available);
            entity.HasOne(s => s.Stock).WithMany(s => s.StockItems).HasForeignKey(s => s.StockId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Product).WithMany(s => s.StockItems).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureShipment(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Shipment>();
            entity.ToTable("shipment");
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Warehouse).WithMany(w => w.Shipments).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Suplier).WithMany(s => s.Shipments).HasForeignKey(s => s.SuplierId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Stock).WithMany(s => s.Shipments).HasForeignKey(s => s.StockId).OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureShipmentItem(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ShipmentItem>();
            entity.ToTable("shipmentItem");
            entity.HasKey(s => new
            {
                s.ProducId,
                s.ShipmentId
            });

            entity.HasOne(s => s.Shipment).WithMany(s => s.ShipmentItems).HasForeignKey(s => s.ShipmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Product).WithMany(s => s.ShipmentItems).HasForeignKey(s => s.ProducId).OnDelete(DeleteBehavior.Cascade);
        }

    }
}