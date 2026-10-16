using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Api.Contracts;
using Warehouse.Api.Data;
using Warehouse.Api.Domain;
using Warehouse.Api.Exceptions;
using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.Services;

public sealed class WarehouseService(WarehouseDbContext db, DomainEventLogger events, RequestExecution execution, IOptions<LabOptions> options, ILogger<WarehouseService> logger)
{

    private static readonly object _reservationLock = new();
    public async Task<ProductResponse> AddProductAsync(AddProductRequest request, CancellationToken ct)
    {
        var sku = request.Sku?.Trim();
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 100)
        {
            throw new IncorrectInputDataException("SKU must contain 1 to 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            throw new IncorrectInputDataException("Product name must contain 1 to 50 characters.");
        }

        if (await db.Products.AnyAsync(x => x.Sku == sku, ct))
        {
            throw new IncorrectInputDataException($"Product with SKU '{sku}' already exists.");
        }

        var product = new Product { Sku = sku, Name = name };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return new ProductResponse(product.Id, product.Sku, product.Name);
    }

    //INFO: this method intentionally does not use transactions to demonstrate databse behaviour when an operation is being interrupted midprocess
    public async Task<ShipmentResponse> ReceiveShipmentFailAsync(ReceiveShipmentRequest request, CancellationToken ct)
    {
        ValidateIds(
        request.WarehouseId,
        request.StockId,
        request.SupplierId);


        if (request.Items is not { Count: > 0 } ||
            request.Items.Any(x => x is null))
        {
            throw new IncorrectInputDataException(
                "Shipment must contain at least one product.");
        }

        if (request.Items.Any(
            x => x.ProductId <= 0 || x.Quantity <= 0))
        {
            throw new IncorrectInputDataException(
                "Product IDs and quantities must be positive.");
        }

        if (request.Items.Select(x => x.ProductId)
            .Distinct().Count() != request.Items.Count)
        {
            throw new IncorrectInputDataException(
                "A product occurs more than once in the shipment.");
        }

        await ValidateLocationAsync(
            request.WarehouseId,
            request.StockId,
            ct);

        if (!await db.Supliers.AnyAsync(
            x => x.Id == request.SupplierId,
            ct))
        {
            throw new ObjectMissingException(
                $"Supplier '{request.SupplierId}' was not found.");
        }

        var ids = request.Items
            .Select(x => x.ProductId)
            .ToList();

        var existing = await db.Products
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var missing = ids.FirstOrDefault(id => !existing.Contains(id));

        if (missing != 0)
        {
            throw new ObjectMissingException(
                $"Product '{missing}' was not found.");
        }

        var shipment = new Shipment
        {
            WarehouseId = request.WarehouseId,
            StockId = request.StockId,
            SupplierId = request.SupplierId
        };

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);

        if (options.Value.TransactionFailStep == 1)
        {

            throw new InvalidOperationException(
                $"Artificial failure after saving the shipment. An empty shipment was added with id={shipment.Id}");
        }

        foreach (var item in request.Items)
        {
            shipment.Items.Add(new ShipmentItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });
        }

        await db.SaveChangesAsync(ct);

        if (options.Value.TransactionFailStep == 2)
        {
            throw new InvalidOperationException(
                $"Artificial failure after saving shipment items. Shipment with id={shipment.Id} added. ShipmentItems added");
        }

        var current = await db.StockItems
            .Where(x =>
                x.StockId == request.StockId &&
                ids.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, ct);

        foreach (var item in request.Items)
        {
            if (!current.TryGetValue(item.ProductId, out var stockItem))
            {
                stockItem = new StockItem
                {
                    StockId = request.StockId,
                    ProductId = item.ProductId,
                    Quantity = 0,
                    Reserved = 0
                };

                db.StockItems.Add(stockItem);
                current.Add(item.ProductId, stockItem);
            }

            if ((long)stockItem.Quantity + item.Quantity > int.MaxValue)
            {
                throw new IncorrectInputDataException(
                    "Receiving this quantity would overflow stock.");
            }

            stockItem.Quantity += item.Quantity;
        }

        await db.SaveChangesAsync(ct);

        events.Publish(new ShipmentReceived(
            shipment.Id,
            request.WarehouseId,
            request.StockId,
            DateTimeOffset.UtcNow));

        foreach (var item in request.Items)
        {
            PublishStockUpdated(current[item.ProductId]);
        }

        return new ShipmentResponse(
            shipment.Id,
            request.WarehouseId,
            request.StockId,
            request.SupplierId,
            request.Items.Select(x => new ShipmentItemResponse(
                x.ProductId,
                x.Quantity)).ToList());
    }

    public async Task<ShipmentResponse> ReceiveShipmentAsync(ReceiveShipmentRequest request, CancellationToken ct)
    {
        ValidateIds(
        request.WarehouseId,
        request.StockId,
        request.SupplierId);

        if (request.Items is not { Count: > 0 } ||
            request.Items.Any(x => x is null))
        {
            throw new IncorrectInputDataException(
                "Shipment must contain at least one product.");
        }

        if (request.Items.Any(
            x => x.ProductId <= 0 || x.Quantity <= 0))
        {
            throw new IncorrectInputDataException(
                "Product IDs and quantities must be positive.");
        }

        if (request.Items.Select(x => x.ProductId)
            .Distinct().Count() != request.Items.Count)
        {
            throw new IncorrectInputDataException(
                "A product occurs more than once in the shipment.");
        }

        await ValidateLocationAsync(
            request.WarehouseId,
            request.StockId,
            ct);

        if (!await db.Supliers.AnyAsync(
            x => x.Id == request.SupplierId,
            ct))
        {
            throw new ObjectMissingException(
                $"Supplier '{request.SupplierId}' was not found.");
        }

        var ids = request.Items
            .Select(x => x.ProductId)
            .ToList();

        var existing = await db.Products
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var missing = ids.FirstOrDefault(id => !existing.Contains(id));

        if (missing != 0)
        {
            throw new ObjectMissingException(
                $"Product '{missing}' was not found.");
        }

        var shipment = new Shipment
        {
            WarehouseId = request.WarehouseId,
            StockId = request.StockId,
            SupplierId = request.SupplierId
        };

        var current = new Dictionary<int, StockItem>();

        await using var transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            db.Shipments.Add(shipment);
            await db.SaveChangesAsync(ct);

            if (options.Value.TransactionFailStep == 1)
            {
                throw new InvalidOperationException(
                    $"Artificial failure after saving the shipment. Attempted to create a shipment with id={shipment.Id}");
            }

            foreach (var item in request.Items)
            {
                shipment.Items.Add(new ShipmentItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await db.SaveChangesAsync(ct);

            if (options.Value.TransactionFailStep== 2)
            {
                throw new InvalidOperationException(
                    $"Artificial failure after saving shipment items. Shipment created with id={shipment.Id}. Attempted to add ShipmentItems");
            }

            current = await db.StockItems
                .Where(x =>
                    x.StockId == request.StockId &&
                    ids.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, ct);

            foreach (var item in request.Items)
            {
                if (!current.TryGetValue(item.ProductId, out var stockItem))
                {
                    stockItem = new StockItem
                    {
                        StockId = request.StockId,
                        ProductId = item.ProductId,
                        Quantity = 0,
                        Reserved = 0
                    };

                    db.StockItems.Add(stockItem);
                    current.Add(item.ProductId, stockItem);
                }

                if ((long)stockItem.Quantity + item.Quantity > int.MaxValue)
                {
                    throw new IncorrectInputDataException(
                        "Receiving this quantity would overflow stock.");
                }

                stockItem.Quantity += item.Quantity;
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            execution.Write(logger,"COMMIT","");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            execution.Write(logger,"ROLLBACK","");
            throw;
        }

        events.Publish(new ShipmentReceived(
            shipment.Id,
            request.WarehouseId,
            request.StockId,
            DateTimeOffset.UtcNow));

        foreach (var item in request.Items)
        {
            PublishStockUpdated(current[item.ProductId]);
        }

        return new ShipmentResponse(
            shipment.Id,
            request.WarehouseId,
            request.StockId,
            request.SupplierId,
            request.Items.Select(x => new ShipmentItemResponse(
                x.ProductId,
                x.Quantity)).ToList());
    }


    //INFO: this method intentionally copies the already existing SyncReserve method to statisfy the 
    //requirements of Lab4
    public async Task<StockResponse> ReserveProductAsync(ReserveProductRequest request, CancellationToken ct)
    {
        execution.Write(logger, "START");
        if (request.WarehouseId <= 0 || request.StockId <= 0 || request.ProductId <= 0 || request.Quantity <= 0)
        {
            throw new IncorrectInputDataException("IDs and quantity must be positive.");
        }

        if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, ct))
        {
            throw new ObjectMissingException($"Warehouse '{request.WarehouseId}' was not found.");
        }

        var stock = await db.Stocks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.StockId, ct);
        if (stock is null)
        {
            throw new ObjectMissingException($"Stock '{request.StockId}' was not found.");
        }

        if (stock.WarehouseId != request.WarehouseId)
        {
            throw new IncorrectInputDataException("Stock does not belong to this warehouse.");
        }

        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId, ct))
        {
            throw new ObjectMissingException($"Product '{request.ProductId}' was not found.");
        }

        StockItem item;

        lock (_reservationLock)
        {
            execution.Write(logger, "LOCK", "");

            item = db.StockItems.SingleOrDefault(x => x.StockId == request.StockId && x.ProductId == request.ProductId);
            if (item is null)
            {
                throw new ObjectMissingException("Product is not present in this stock.");
            }

            if (item.Available < request.Quantity)
            {
                execution.Write(logger, "FAIL", $"Requested {request.Quantity}; available {item.Available}.");
                throw new IncorrectInputDataException($"Requested {request.Quantity}; available {item.Available}.");
            }

            execution.Write(logger, "CHECK", $"Product={item.ProductId} Quantity={item.Quantity} " + $"Reserved={item.Reserved} Available={item.Available}");

            ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(options.Value.RaceDelaySeconds));

            ct.ThrowIfCancellationRequested();

            item.Reserved += request.Quantity;
            db.SaveChanges();

            execution.Write(logger, "SUCCESS", $"Reserved={item.Reserved} Available={item.Available}");
            execution.Write(logger, "UNLOCK", $"ProductId={request.ProductId}");

        }
        events.Publish(new ProductReserved(request.WarehouseId, request.StockId,
            request.ProductId, request.Quantity, DateTimeOffset.UtcNow));

        events.Publish(new StockUpdated(item.StockId, item.ProductId, item.Quantity,
            item.Reserved, item.Available, DateTimeOffset.UtcNow));

        return await GetStockAsync(request.StockId, ct);
    }

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
        if (stock is null)
        {
            throw new ObjectMissingException($"Stock '{stockId}' was not found.");
        }

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
        {
            throw new ObjectMissingException("Run db/seed.sql to create the demo warehouse and supplier.");
        }

        var stock = await db.Stocks.AsNoTracking().OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouse.Id && x.Name == "Main Sorting Zone", ct);

        if (stock is null)
        {
            throw new ObjectMissingException("Run db/seed.sql to create the demo stock.");
        }

        return new BootstrapResponse(warehouse.Id, stock.Id, supplier.Id);
    }

    public async Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(
    CancellationToken ct)
    {
        return await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name))
            .ToListAsync(ct);
    }

    public async Task<SupplierResponse> AddSupplierAsync(
    AddSupplierRequest request,
    CancellationToken ct)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            throw new IncorrectInputDataException(
                "Supplier name must contain 1 to 50 characters.");
        }

        if (await db.Supliers.AnyAsync(
            supplier => supplier.Name == name,
            ct))
        {
            throw new IncorrectInputDataException(
                $"Supplier with name '{name}' already exists.");
        }

        var supplier = new Suplier
        {
            Name = name
        };

        db.Supliers.Add(supplier);

        await db.SaveChangesAsync(ct);

        return new SupplierResponse(
            supplier.Id,
            supplier.Name);
    }

    public async Task<IReadOnlyCollection<SupplierResponse>> GetSuppliersAsync(
        CancellationToken ct)
    {
        return await db.Supliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Name))
            .ToListAsync(ct);
    }

    public Task<ServerTimeResponse> GetServerTimeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new ServerTimeResponse(DateTime.Now.ToString()));
    }

    private async Task ValidateLocationAsync(int warehouseId, int stockId, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId, ct))
        {
            throw new ObjectMissingException($"Warehouse '{warehouseId}' was not found.");
        }

        var stock = await db.Stocks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == stockId, ct);

        if (stock is null)
        {
            throw new ObjectMissingException($"Stock '{stockId}' was not found.");
        }
        if (stock.WarehouseId != warehouseId)
        {
            throw new IncorrectInputDataException("Stock does not belong to the specified warehouse.");
        }

    }

    private static void ValidateIds(params int[] ids)
    {
        if (ids.Any(x => x <= 0))
        {
            throw new IncorrectInputDataException("All IDs must be positive integers.");
        }

    }

    private async Task LockStockProductAsync(int stockId, int productId, CancellationToken ct)
    {

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({stockId}, {productId})", ct);
    }

    private void PublishStockUpdated(StockItem item) => events.Publish(new StockUpdated(
        item.StockId, item.ProductId, item.Quantity, item.Reserved, item.Available, DateTimeOffset.UtcNow));
}
