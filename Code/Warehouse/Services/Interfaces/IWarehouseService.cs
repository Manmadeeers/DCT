using Warehouse.Models.DTOs;
using Warehouse.Models.Entities;

namespace Warehouse.Services.Interfaces;

public interface IWarehouseService
{
    // Product operations
    Task<Product> AddProductAsync(CreateProductRequest request);
    Task<Product> GetProductByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);

    // Stock operations
    Task<Stock> CreateStockAsync(CreateStockRequest request);
    Task<Stock> GetStockByIdAsync(int id);
    Task<IEnumerable<Stock>> GetAllStocksAsync();
    Task<IEnumerable<Stock>> GetStocksByWarehouseAsync(int warehouseId);
    Task<Stock> UpdateStockAsync(int id, UpdateStockRequest request);
    Task<bool> DeleteStockAsync(int id);

    // Shipment operations
    Task<Shipment> CreateShipmentAsync(CreateShippmentRequest request);
    Task<Shipment> ReceiveShipmentAsync(int shipmentId, ReceiveShipmentRequest request);
    Task<Shipment> GetShipmentByIdAsync(int id);
    Task<IEnumerable<Shipment>> GetAllShipmentsAsync();
    Task<IEnumerable<Shipment>> GetShipmentsByWarehouseAsync(int warehouseId);

    // Reserve operations
    Task<bool> ReserveProductAsync(ReserveProductRequest request);
    Task<bool> ShipProductAsync(ShipProductRequest request);

    // Supplier operations
    Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request);
    Task<Supplier> GetSupplierByIdAsync(int id);
    Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
    Task<Supplier> UpdateSupplierAsync(int id, UpdateSupplierRequest request);
    Task<bool> DeleteSupplierAsync(int id);

    // Warehouse operations
    Task<Models.Entities.Warehouse> CreateWarehouseAsync(CreateWarehouseRequest request);
    Task<Models.Entities.Warehouse> GetWarehouseByIdAsync(int id);
    Task<IEnumerable<Models.Entities.Warehouse>> GetAllWarehousesAsync();
    Task<Models.Entities.Warehouse> UpdateWarehouseAsync(int id, UpdateWarehouseRequest request);
    Task<bool> DeleteWarehouseAsync(int id);
}