using Warehouse.Api.Data;
using Warehouse.Api.Infrastructure;
using Microsoft.Extensions.Options;
using Warehouse.Api.Contracts;
using Warehouse.Api.Exceptions;
using Warehouse.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Api.Services;


public sealed class TransactionLabService(WarehouseDbContext db, DomainEventLogger events, IOptions<LabOptions> options)
{

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



    private void PublishStockUpdated(StockItem item) => events.Publish(new StockUpdated(
        item.StockId, item.ProductId, item.Quantity, item.Reserved, item.Available, DateTimeOffset.UtcNow));
    private static void ValidateIds(params int[] ids)
    {
        if (ids.Any(x => x <= 0))
        {
            throw new IncorrectInputDataException("All IDs must be positive integers.");
        }

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

}
