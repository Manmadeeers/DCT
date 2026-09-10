using Microsoft.EntityFrameworkCore;
using Warehouse.Models.Entities;

namespace Warehouse.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Warehouse.Models.Entities.Warehouse> Warehouses { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Shippment> Shipments { get; set; }
    public DbSet<ShippingItem> ShippingItems { get; set; }
    public DbSet<Stock> Stocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasDefaultValue(0);
            entity.Property(e => e.Created_at).HasDefaultValueSql("now()");

            // Relationship: Product → Stock (Many-to-One)
            entity.HasOne(e => e.Stock)
                  .WithMany(s => s.Products)
                  .HasForeignKey(e => e.Stock_id)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Warehouse configuration
        modelBuilder.Entity<Warehouse.Models.Entities.Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
            entity.Property(e => e.City).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Capacity).HasDefaultValue(0);
            entity.Property(e => e.Created_at).HasDefaultValueSql("now()");
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.Property(e => e.Address).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Address).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Created_at).HasDefaultValueSql("now()");
        });

        // Stock configuration
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Quantity).HasDefaultValue(0);
            entity.Property(e => e.Reserved_quantity).HasDefaultValue(0);
            entity.Property(e => e.Created_at).HasDefaultValueSql("now()");

            // Relationship: Stock → Warehouse (Many-to-One)
            entity.HasOne(e => e.Warehouse)
                  .WithMany(w => w.Stocks)
                  .HasForeignKey(e => e.Warehouse_id)
                  .OnDelete(DeleteBehavior.Restrict);

            // Each stock name must be unique within a warehouse
            entity.HasIndex(e => new { e.Name, e.Warehouse_id }).IsUnique();
        });

        // Shipment configuration
        modelBuilder.Entity<Shippment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Shipment_number).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Shipment_number).IsUnique();
            entity.Property(e => e.Tracking_number).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Tracking_number).IsUnique();
            entity.Property(e => e.Shipping_fee).HasDefaultValue(0);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.Created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.Shipment_status).HasConversion<string>();

            // Relationships
            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.Shippments)
                  .HasForeignKey(e => e.Supplier_id)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Warehouse)
                  .WithMany(w => w.Shippments)
                  .HasForeignKey(e => e.Warehouse_id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShippingItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasDefaultValue(0);
            entity.Property(e => e.Unit_cost).HasDefaultValue(0);

            // Relationships
            entity.HasOne(e => e.Shippment)
                  .WithMany(s => s.ShippingItems)
                  .HasForeignKey(e => e.Shipment_id)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.ShippingItem)
                  .HasForeignKey(e => e.Product_id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Add default warehouse
        modelBuilder.Entity<Warehouse.Models.Entities.Warehouse>().HasData(
            new Warehouse.Models.Entities.Warehouse
            {
                Id = 1,
                Name = "Main Warehouse",
                Country = "USA",
                City = "New York",
                Email = "main@warehouse.com",
                Capacity = 10000,
                Created_at = DateTime.UtcNow
            }
        );

        // Add default stocks
        modelBuilder.Entity<Stock>().HasData(
            new Stock
            {
                Id = 1,
                Name = "Dry Zone A",
                Warehouse_id = 1,
                Quantity = 1000,
                Reserved_quantity = 0,
                Created_at = DateTime.UtcNow
            },
            new Stock
            {
                Id = 2,
                Name = "Cold Zone B",
                Warehouse_id = 1,
                Quantity = 500,
                Reserved_quantity = 0,
                Created_at = DateTime.UtcNow
            }
        );

        // Add default supplier
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier
            {
                Id = 1,
                Name = "Tech Supplies Inc",
                Email = "john@techsupplies.com",
                Phone = "+1-555-5678",
                Address = "456 Tech Ave",
                IsActive = true,
                Created_at = DateTime.UtcNow
            }
        );
    }
}