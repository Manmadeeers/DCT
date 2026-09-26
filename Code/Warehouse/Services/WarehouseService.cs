using System.ComponentModel;
using System.Drawing;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Warehouse.Data;
using Warehouse.Models.DTOs;

namespace Warehouse.Services
{
    public class WarehouseService
    {
        private readonly AppDbContext _db;
        public WarehouseService(AppDbContext db) => _db = db;
        public async Task<IReadOnlyList<WarehouseDTO>> GetAllWarehousesAsync(CancellationToken ct = default)
        {
            return await _db.Warehouses.AsNoTracking().OrderBy(w => w.Name).Select(w => new WarehouseDTO
            {
                Id = w.Id,
                Name = w.Name,
                City = w.City,
                Country = w.Country
            }).ToListAsync(ct);
        }

        public async Task<WarehouseDTO?> GetWarehouseByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Warehouses.AsNoTracking().Select(w => new WarehouseDTO
            {
                Id = w.Id,
                Name = w.Name,
                City = w.City,
                Country = w.Country
            }).FirstOrDefaultAsync(ct);
        }

        public async Task<WarehouseDTO> AddWarehouseAsync(CreateWarehouseRequest body, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(body.Name) || String.IsNullOrWhiteSpace(body.City) || String.IsNullOrWhiteSpace(body.Country))
            {
                throw new ArgumentException("Failed to create a warehouse with null or empty fields");
            }

            bool nameTaken = await _db.Warehouses.AnyAsync(w => w.Name == body.Name, ct);
            if (nameTaken)
            {
                throw new InvalidOperationException("Failed to create a warehouse. Name already exists");
            }

            var warehouse = new Warehouse.Models.Entities.Warehouse
            {
                Name = body.Name,
                City = body.City,
                Country = body.Country
            };

            await _db.Warehouses.AddAsync(warehouse, ct);

            await _db.SaveChangesAsync(ct);

            return new WarehouseDTO
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                City = warehouse.City,
                Country = warehouse.Country
            };
        }

        public async Task DeleteWarehouseAsync(int id, CancellationToken ct = default)
        {
            var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);
            if (warehouse is null)
            {
                throw new KeyNotFoundException("Failed to delete a warehouse. Not found");
            }

            var referencedStocks = await _db.Stocks.AnyAsync(s => s.WarehouseId == id, ct);
            if (referencedStocks)
            {
                throw new InvalidOperationException("Failed to delete a warehouse. Some stocks still reference it");
            }

            var referencedShipments = await _db.Shippments.AnyAsync(s => s.WarehouseId == id, ct);
            if (referencedShipments)
            {
                throw new InvalidOperationException("Failed to delete a warehouse. There are still some ongoing shippments");
            }

            _db.Warehouses.Remove(warehouse);
            await _db.SaveChangesAsync(ct);
        }


    }
}