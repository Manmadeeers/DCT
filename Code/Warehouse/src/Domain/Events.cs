namespace Warehouse.Api.Domain;

public interface IDomainEvent;
public sealed record ShipmentReceived(int ShipmentId, int WarehouseId, int StockId,
    DateTimeOffset Timestamp) : IDomainEvent;
public sealed record ProductReserved(int WarehouseId, int StockId, int ProductId,
    int Quantity, DateTimeOffset Timestamp) : IDomainEvent;
public sealed record ProductShipped(int WarehouseId, int StockId, int ProductId,
    int Quantity, DateTimeOffset Timestamp) : IDomainEvent;
public sealed record StockUpdated(int StockId, int ProductId, int Quantity,
    int Reserved, int Available, DateTimeOffset Timestamp) : IDomainEvent;
