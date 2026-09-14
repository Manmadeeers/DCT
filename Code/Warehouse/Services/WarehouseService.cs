using Microsoft.EntityFrameworkCore;
using Warehouse.Data;
using Warehouse.Models.DTOs;
using Warehouse.Models.Entities;
using Warehouse.Services.Interfaces;

namespace Warehouse.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly AppDbContext _db;

        public WarehouseService(AppDbContext db) => _db = db;
        public async Task<ProductDTO> AddProductAsync(CreateProductRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Product name is required");
            }

            if (request.Price <= 0)
            {
                throw new ArgumentException("Product price is required and could not be equal or less then zero");
            }

            if (request.Quantity <= 0)
            {
                throw new ArgumentException("Product quantity could not be equal or less than zero");
            }

            var Stocks = await _db.Stocks.FirstOrDefaultAsync(s => s.Id == request.Stock_id, ct) ?? throw new KeyNotFoundException($"Stock with {request.Stock_id} is not found");

            bool nameTaken = await _db.Products.AnyAsync(p => p.Name == request.Name, ct);
            if (nameTaken)
            {
                throw new InvalidOperationException($"Product with name {request.Name} already exists");
            }

            var product = new Product
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                Quantity = request.Quantity,
                StockId = Stocks.Id
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);

            return new ProductDTO
            {
                Id = product.Id,
                Name=product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity= product.Quantity,
                Stock_id = product.StockId
            };
        }
    }
}