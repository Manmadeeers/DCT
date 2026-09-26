using Microsoft.AspNetCore.RateLimiting;

namespace Warehouse.Events
{
    public interface IWarehouseEvent;

    public sealed record ShipmentReceived(
        int ShipmentId,
        int WarehouseId,
        int StockId
    ):IWarehouseEvent;

    public sealed record ProductReserved(
        int WarehouseId,
        int StockId,
        int ProductId,
        int Quantity
    ):IWarehouseEvent;

    public sealed record ProductShiped(
        int WarehouseId,
        int StockId,
        int ProductId,
        int Quantity
    ):IWarehouseEvent;

    public sealed record StockUpdated(
        int StockId,
        int ProductId,
        int Quantity,
        int Reserved,
        int Available
    ):IWarehouseEvent;
}