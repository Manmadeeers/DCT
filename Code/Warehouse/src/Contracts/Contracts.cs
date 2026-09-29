namespace Warehouse.Api.Contracts;

public sealed record AddProductRequest(string? Sku, string? Name);
public sealed record ProductResponse(int Id, string Sku, string Name);
public sealed record ShipmentItemRequest(int ProductId, int Quantity);
public sealed record ReceiveShipmentRequest(int WarehouseId, int StockId, int SupplierId,
    List<ShipmentItemRequest>? Items);
public sealed record ShipmentItemResponse(int ProductId, int Quantity);
public sealed record ShipmentResponse(int Id, int WarehouseId, int StockId, int SupplierId,
    IReadOnlyCollection<ShipmentItemResponse> Items);
public sealed record ReserveProductRequest(int WarehouseId, int StockId, int ProductId, int Quantity);
public sealed record ShipProductRequest(int WarehouseId, int StockId, int ProductId, int Quantity);
public sealed record StockItemResponse(int ProductId, int Quantity, int Reserved, int Available);
public sealed record StockResponse(int Id, int? WarehouseId, string Name,
    IReadOnlyCollection<StockItemResponse> Items);
public sealed record BootstrapResponse(int WarehouseId, int StockId, int SupplierId);

public sealed record AddSupplierRequest(string? Name);

public sealed record SupplierResponse(
    int Id,
    string Name);
