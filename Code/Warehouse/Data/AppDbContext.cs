using Microsoft.EntityFrameworkCore;
using Npgsql.Internal;
using Warehouse.Models.DTOs;
using Warehouse.Models.Entities;

namespace Warehouse.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Models.Entities.Warehouse> Warehouses => Set<Models.Entities.Warehouse>();
        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Shippment> Shippments => Set<Shippment>();
        public DbSet<ShippingItem> ShippingItems => Set<ShippingItem>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.Entities.Warehouse>(e =>
            {
                e.ToTable("Warehouse");
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).HasColumnName("Name").HasMaxLength(50).IsRequired();
                e.Property(w => w.Country).HasColumnName("Country").HasMaxLength(50).IsRequired();
                e.Property(w => w.City).HasColumnName("City").HasMaxLength(50).IsRequired();

                e.HasIndex(w => w.Name).IsUnique();

            });

            modelBuilder.Entity<Stock>(e =>
            {
                e.ToTable("Stock");
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).HasColumnName("Name").HasMaxLength(100).IsRequired();
                e.Property(w => w.StockUnits).HasColumnName("StockUnits").IsRequired();
                e.Property(w => w.ReservedUnits).HasColumnName("ReservedUnits").IsRequired();
                e.Ignore(w => w.AvailableUnits);

                e.HasOne(s => s.Warehouse).WithMany(w => w.Stocks).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("Product");
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).HasColumnName("Name").HasMaxLength(100).IsRequired();
                e.Property(w => w.Description).HasColumnName("Description").HasMaxLength(500);
                e.Property(w => w.Price).HasColumnName("Price").IsRequired();
                e.Property(w => w.Quantity).HasColumnName("Quantity").IsRequired();

                e.HasIndex(w => w.Name).IsUnique();

                e.HasOne(p => p.Stock).WithMany(s => s.Products).HasForeignKey(p => p.StockId).OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Supplier>(e =>
            {
                e.ToTable("Supplier");
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).HasColumnName("Name").HasMaxLength(50).IsRequired();
                e.Property(w => w.Email).HasColumnName("Email").HasMaxLength(100).IsRequired();
                e.Property(w => w.Phone).HasColumnName("Phone").HasMaxLength(20).IsRequired();
                e.Property(w => w.Address).HasColumnName("Address").HasMaxLength(100).IsRequired();

                e.HasIndex(w => w.Name).IsUnique();
                e.HasIndex(w => w.Email).IsUnique();
                e.HasIndex(w => w.Phone).IsUnique();
                e.HasIndex(w => w.Address).IsUnique();
            });

            modelBuilder.Entity<Shippment>(e =>
            {
                e.ToTable("Shipment");
                e.HasKey(w => w.Id);
                e.Property(w => w.ShipmentNumber).HasColumnName("ShipmentNumber").HasMaxLength(100).IsRequired();
                e.Property(w => w.ExpectedDate).HasColumnName("ExpectedDate").IsRequired();
                e.Property(w => w.ReceivedDate).HasColumnName("ReceivedDate");
                e.Property(w => w.ShipmentStatus).HasColumnName("ShipmentStatus").HasMaxLength(10).IsRequired();

                e.HasIndex(w => w.ShipmentNumber).IsUnique();

                e.HasOne(s => s.Supplier).WithMany(sp => sp.Shippments).HasForeignKey(s => s.SupplierId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Warehouse).WithMany(wh => wh.Shippments).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShippingItem>(e =>
            {
                e.ToTable("ShippingItem");
                e.HasKey(w => w.Id);
                e.Property(w => w.Quantity).IsRequired();
                e.Property(w => w.UnitCost).IsRequired();

                e.HasOne(s => s.Shippment).WithMany(sp => sp.ShippingItems).HasForeignKey(s => s.ShippmentId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Product).WithMany(p => p.ShippingItems).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}