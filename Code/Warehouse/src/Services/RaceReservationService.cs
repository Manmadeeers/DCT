using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Api.Contracts;
using Warehouse.Api.Data;
using Warehouse.Api.Domain;
using Warehouse.Api.Exceptions;
using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.Services;

public sealed class RaceReservationService(
    WarehouseDbContext db,
    WarehouseService warehouse,
    DomainEventLogger events,
    RequestExecution execution,
    IOptions<LabOptions> options,
    ILogger<RaceReservationService> logger)
{
    public async Task<StockResponse> ReserveAsync(ReserveProductRequest request, CancellationToken ct)
    {
        if (request.WarehouseId <= 0 || request.StockId <= 0 ||
            request.ProductId <= 0 || request.Quantity <= 0)
            throw new IncorrectInputDataException("IDs and quantity must be positive.");
        if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, ct))
            throw new ObjectMissingException($"Warehouse '{request.WarehouseId}' was not found.");
        var stock = await db.Stocks.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.StockId, ct);
        if (stock is null)
            throw new ObjectMissingException($"Stock '{request.StockId}' was not found.");
        if (stock.WarehouseId != request.WarehouseId)
            throw new IncorrectInputDataException("Stock does not belong to this warehouse.");
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId, ct))
            throw new ObjectMissingException($"Product '{request.ProductId}' was not found.");
        var item = await db.StockItems.SingleOrDefaultAsync(
            x => x.StockId == request.StockId && x.ProductId == request.ProductId, ct);
        if (item is null)
            throw new ObjectMissingException("Product is not present in this stock.");
        if (item.Available < request.Quantity)
            throw new IncorrectInputDataException(
                $"Requested {request.Quantity}; available {item.Available}.");

        execution.Write(logger, "CHECK",
            $"Product={item.ProductId} Quantity={item.Quantity} " +
            $"Reserved={item.Reserved} Available={item.Available}");
        // No lock, transaction around the check, or concurrency token.
        // Each request has its own tracked snapshot of the same database row.
        await Task.Delay(TimeSpan.FromSeconds(options.Value.RaceDelaySeconds), ct);
        item.Reserved += request.Quantity;
        await db.SaveChangesAsync(ct);
        execution.Write(logger, "WRITE", $"Reserved={item.Reserved} Available={item.Available}");
        events.Publish(new ProductReserved(request.WarehouseId, request.StockId,
            request.ProductId, request.Quantity, DateTimeOffset.UtcNow));
        events.Publish(new StockUpdated(item.StockId, item.ProductId, item.Quantity,
            item.Reserved, item.Available, DateTimeOffset.UtcNow));
        return await warehouse.GetStockAsync(request.StockId, ct);
    }
}