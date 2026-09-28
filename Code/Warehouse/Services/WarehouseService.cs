using Microsoft.EntityFrameworkCore;
using Warehouse.Data;
using Warehouse.Events;
using Warehouse.Exceptions;
using Warehouse.Models.Contracts;
using Warehouse.Models.Entities;

namespace Warehouse.Services
{
    public sealed class WarehouseService(WarehouseDbContext context, WarehouseEventLogger logger)
    {
        public async Task<ProductResponse>AddProductAsync(AddProductRequest request, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(request.Sku) || String.IsNullOrWhiteSpace(request.Name))
            {
                throw new IncorrectInputDataException("Stock keeping unit or product's name were null or empty");
            }

            var sku = request.Sku.Trim();
            var name = request.Name.Trim();

            var  alreadyExists = await context.Products.AnyAsync(p=>p.Sku==sku,ct);
            if (alreadyExists)
            {
                throw new InvalidOperationException($"Product with SKU {sku} already exists");
            }

            var product = new Product
            {
                Sku=sku,
                Name=name
            };
            
            await context.Products.AddAsync(product,ct);

            await context.SaveChangesAsync(ct);

            return new ProductResponse(product.Id,product.Sku,product.Name);
        }

        public async Task<ShipmentResponse>ReceiveShipmentAsync(ReceiveShipmentRequest request, CancellationToken ct = default)
        {
            if (request.WarehouseId <= 0 || request.StockId <= 0 || request.SuplierId <= 0)
            {
                throw new IncorrectInputDataException("Requested Ids were invalid");
            }

            if(request.Items is null || request.Items.Count == 0)
            {
                throw new IncorrectInputDataException("Attempted to create a shipment with null or empty items");
            }

            if (request.Items.Any(p => p.Id <= 0))
            {
                throw new IncorrectInputDataException("One or many items in a shipment had invalid ids");
            }

            var duplicateProduct = request.Items.GroupBy(p=>p.Id).FirstOrDefault(p=>p.Count()>1);
            if(duplicateProduct is not null)
            {
                throw new IncorrectInputDataException("One or many products were duplicated in a shipment");
            }

            var warehouse = await context.Warehouses.FirstOrDefaultAsync(w=>w.Id==request.WarehouseId,ct)?? throw new ObjectMissingException("Warehouse with desired id was not found. Unable to create a shipment");
            var stock = await context.Stocks.FirstOrDefaultAsync(s=>s.Id==request.StockId,ct)?? throw new ObjectMissingException("Stock with desired id was not found. Unable to create a shipment");
            var suplier = await context.Supliers.FirstOrDefaultAsync(s=>s.Id==request.SuplierId)??throw new ObjectMissingException("Suplier with desired id was not found. Unable to create a shipment");

            if (stock.WarehouseId != request.WarehouseId)
            {
                throw new IncorrectInputDataException("Requested stock's warehouse id and requested warehouse id does not match");
            }

            var productIds = request.Items.Select(p=>p.Id).ToList();
            var existingProducts = await context.Products.Where(p=>productIds.Contains(p.Id)).Select(p=>p.Id).ToListAsync(ct);
            var missingProducts = productIds.FirstOrDefault(id=>!existingProducts.Contains(id));
            if(missingProducts != default)
            {
                throw new ObjectMissingException($"Product {missingProducts} was not found");
            }

        }
    }
}