using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Contracts;
using Warehouse.Api.Data;
using Warehouse.Api.Domain;
using Warehouse.Api.Exceptions;
using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.Services;

public sealed class WarehouseService(WarehouseDbContext db, DomainEventLogger events)
{
    public async Task<ProductResponse> AddProductAsync(AddProductRequest request, CancellationToken ct)
    {
        var sku = request.Sku?.Trim();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 100)
            throw new IncorrectInputDataException("SKU must contain 1 to 100 characters.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            throw new IncorrectInputDataException("Product name must contain 1 to 50 characters.");
        if (await db.Products.AnyAsync(x => x.Sku == sku, ct))
            throw new IncorrectInputDataException($"Product with SKU '{sku}' already exists.");

        var product = new Product { Sku = sku, Name = name };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return new ProductResponse(product.Id, product.Sku, product.Name);
    }

    public async Task<ShipmentResponse> ReceiveShipmentAsync(ReceiveShipmentRequest request, CancellationToken ct)
    {
        ValidateIds(request.WarehouseId, request.StockId, request.SupplierId);
        if (request.Items is not { Count: > 0 } || request.Items.Any(x => x is null))
            throw new IncorrectInputDataException("Shipment must contain at least one product.");
        if (request.Items.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
            throw new IncorrectInputDataException("Product IDs and quantities must be positive.");
        if (request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
            throw new IncorrectInputDataException("A product occurs more than once in the shipment.");

        await ValidateLocationAsync(request.WarehouseId, request.StockId, ct);
        if (!await db.Supliers.AnyAsync(x => x.Id == request.SupplierId, ct))
            throw new ObjectMissingException($"Supplier '{request.SupplierId}' was not found.");
        var ids = request.Items.Select(x => x.ProductId).ToList();
        var existing = await db.Products.Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        var missing = ids.FirstOrDefault(id => !existing.Contains(id));
        if (missing != 0) throw new ObjectMissingException($"Product '{missing}' was not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A stable lock order prevents concurrent first inserts and deadlocks between shipments.
        foreach (var productId in ids.OrderBy(x => x))
            await LockStockProductAsync(request.StockId, productId, ct);
        var current = await db.StockItems
            .Where(x => x.StockId == request.StockId && ids.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, ct);
        var shipment = new Shipment
        {
            WarehouseId = request.WarehouseId,
            StockId = request.StockId,
            SupplierId = request.SupplierId
        };
        foreach (var item in request.Items)
        {
            shipment.Items.Add(new ShipmentItem { ProductId = item.ProductId, Quantity = item.Quantity });
            if (!current.TryGetValue(item.ProductId, out var stockItem))
            {
                stockItem = new StockItem
                {
                    StockId = request.StockId, ProductId = item.ProductId,
                    Quantity = 0, Reserved = 0
                };
                db.StockItems.Add(stockItem);
                current.Add(item.ProductId, stockItem);
            }
            if ((long)stockItem.Quantity + item.Quantity > int.MaxValue)
                throw new IncorrectInputDataException("Receiving this quantity would overflow stock.");
            stockItem.Quantity += item.Quantity;
        }
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        events.Publish(new ShipmentReceived(shipment.Id, request.WarehouseId, request.StockId,
            DateTimeOffset.UtcNow));
        foreach (var item in request.Items) PublishStockUpdated(current[item.ProductId]);
        return new ShipmentResponse(shipment.Id, request.WarehouseId, request.StockId,
            request.SupplierId, request.Items.Select(x => new ShipmentItemResponse(x.ProductId, x.Quantity)).ToList());
    }

    public Task<StockResponse> ReserveProductAsync(ReserveProductRequest request, CancellationToken ct) =>
        ChangeStockAsync(request.WarehouseId, request.StockId, request.ProductId, request.Quantity, false, ct);

    public Task<StockResponse> ShipProductAsync(ShipProductRequest request, CancellationToken ct) =>
        ChangeStockAsync(request.WarehouseId, request.StockId, request.ProductId, request.Quantity, true, ct);

    private async Task<StockResponse> ChangeStockAsync(int warehouseId, int stockId, int productId,
        int quantity, bool shipping, CancellationToken ct)
    {
        ValidateIds(warehouseId, stockId, productId);
        if (quantity <= 0) throw new IncorrectInputDataException("Quantity must be positive.");
        await ValidateLocationAsync(warehouseId, stockId, ct);
        if (!await db.Products.AnyAsync(x => x.Id == productId, ct))
            throw new ObjectMissingException($"Product '{productId}' was not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockStockProductAsync(stockId, productId, ct);
        var item = await db.StockItems.SingleOrDefaultAsync(
            x => x.StockId == stockId && x.ProductId == productId, ct);
        if (item is null)
            throw new ObjectMissingException($"Product '{productId}' is not present in this stock.");
        if (shipping)
        {
            if (item.Reserved < quantity)
                throw new IncorrectInputDataException(
                    $"Cannot ship {quantity} units; only {item.Reserved} are reserved.");
            item.Reserved -= quantity;
            item.Quantity -= quantity;
        }
        else
        {
            if (item.Available < quantity)
                throw new IncorrectInputDataException(
                    $"Not enough product available; requested {quantity}, available {item.Available}.");
            item.Reserved += quantity;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        if (shipping)
            events.Publish(new ProductShipped(warehouseId, stockId, productId, quantity, DateTimeOffset.UtcNow));
        else
            events.Publish(new ProductReserved(warehouseId, stockId, productId, quantity, DateTimeOffset.UtcNow));
        PublishStockUpdated(item);
        return await GetStockAsync(stockId, ct);
    }

    public async Task<StockResponse> GetStockAsync(int stockId, CancellationToken ct)
    {
        var stock = await db.Stocks.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == stockId, ct);
        if (stock is null) throw new ObjectMissingException($"Stock '{stockId}' was not found.");
        return new StockResponse(stock.Id, stock.WarehouseId, stock.Name,
            stock.Items.OrderBy(x => x.ProductId)
                .Select(x => new StockItemResponse(x.ProductId, x.Quantity, x.Reserved, x.Available)).ToList());
    }

    public async Task<BootstrapResponse> GetBootstrapAsync(CancellationToken ct)
    {
        var warehouse = await db.Warehouses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == "Central Warehouse", ct);
        var supplier = await db.Supliers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == "Main Supplier", ct);
        if (warehouse is null || supplier is null)
            throw new ObjectMissingException("Run db/seed.sql to create the demo warehouse and supplier.");
        var stock = await db.Stocks.AsNoTracking().OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouse.Id && x.Name == "Main Sorting Zone", ct);
        if (stock is null)
            throw new ObjectMissingException("Run db/seed.sql to create the demo stock.");
        return new BootstrapResponse(warehouse.Id, stock.Id, supplier.Id);
    }

    private async Task ValidateLocationAsync(int warehouseId, int stockId, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId, ct))
            throw new ObjectMissingException($"Warehouse '{warehouseId}' was not found.");
        var stock = await db.Stocks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == stockId, ct);
        if (stock is null) throw new ObjectMissingException($"Stock '{stockId}' was not found.");
        if (stock.WarehouseId != warehouseId)
            throw new IncorrectInputDataException("Stock does not belong to the specified warehouse.");
    }

    private static void ValidateIds(params int[] ids)
    {
        if (ids.Any(x => x <= 0))
            throw new IncorrectInputDataException("All IDs must be positive integers.");
    }

    private async Task LockStockProductAsync(int stockId, int productId, CancellationToken ct)
    {
        
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({stockId}, {productId})", ct);
    }

    private void PublishStockUpdated(StockItem item) => events.Publish(new StockUpdated(
        item.StockId, item.ProductId, item.Quantity, item.Reserved, item.Available, DateTimeOffset.UtcNow));
}
