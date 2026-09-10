using Microsoft.EntityFrameworkCore;
using Warehouse.Data;
using Warehouse.Models.DTOs;
using Warehouse.Models.Entities;
using Warehouse.Services.Interfaces;

namespace Warehouse.Services;

public class WarehouseService : IWarehouseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(AppDbContext context, ILogger<WarehouseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Product Operations

    public async Task<Product> AddProductAsync(CreateProductRequest request)
    {
        // Check if product name already exists (unique constraint)
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Name == request.Name);

        if (existingProduct != null)
            throw new InvalidOperationException($"Product with name '{request.Name}' already exists");

        // Validate stock exists if provided
        if (request.Stock_id.HasValue)
        {
            var stock = await _context.Stocks.FindAsync(request.Stock_id.Value);
            if (stock == null)
                throw new KeyNotFoundException($"Stock with ID {request.Stock_id.Value} not found");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock_id = request.Stock_id,
            Created_at = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Product '{product.Name}' created with ID {product.Id}");
        return product;
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        return product;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Stock)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        if (request.Name != null)
        {
            // Check if new name conflicts with another product
            var existing = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == request.Name && p.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Product with name '{request.Name}' already exists");

            product.Name = request.Name;
        }

        if (request.Description != null)
            product.Description = request.Description;

        if (request.Price.HasValue)
            product.Price = request.Price.Value;

        if (request.Stock_Id.HasValue)
        {
            var stock = await _context.Stocks.FindAsync(request.Stock_Id.Value);
            if (stock == null)
                throw new KeyNotFoundException($"Stock with ID {request.Stock_Id.Value} not found");
            product.Stock_id = request.Stock_Id;
        }

        product.Updated_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Product {product.Id} updated");
        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.ShippingItem)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return false;

        if (product.ShippingItem.Any())
            throw new InvalidOperationException($"Cannot delete product '{product.Name}' because it is referenced in shipments");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Product {id} deleted");
        return true;
    }

    #endregion

    #region Stock Operations

    public async Task<Stock> CreateStockAsync(CreateStockRequest request)
    {
        // Validate warehouse exists
        var warehouse = await _context.Warehouses.FindAsync(request.Warehouse_id);
        if (warehouse == null)
            throw new KeyNotFoundException($"Warehouse with ID {request.Warehouse_id} not found");

        // Check if stock name already exists in this warehouse
        var existingStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Name == request.Name && s.Warehouse_id == request.Warehouse_id);

        if (existingStock != null)
            throw new InvalidOperationException($"Stock '{request.Name}' already exists in this warehouse");

        var stock = new Stock
        {
            Name = request.Name,
            Warehouse_id = request.Warehouse_id,
            Quantity = request.Quantity,
            Reserved_quantity = 0,
            Created_at = DateTime.UtcNow
        };

        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Stock '{stock.Name}' created with ID {stock.Id}");
        return stock;
    }

    public async Task<Stock> GetStockByIdAsync(int id)
    {
        var stock = await _context.Stocks
            .Include(s => s.Warehouse)
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stock == null)
            throw new KeyNotFoundException($"Stock with ID {id} not found");

        return stock;
    }

    public async Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        return await _context.Stocks
            .Include(s => s.Warehouse)
            .Include(s => s.Products)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Stock>> GetStocksByWarehouseAsync(int warehouseId)
    {
        return await _context.Stocks
            .Include(s => s.Warehouse)
            .Include(s => s.Products)
            .Where(s => s.Warehouse_id == warehouseId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Stock> UpdateStockAsync(int id, UpdateStockRequest request)
    {
        var stock = await _context.Stocks.FindAsync(id);
        if (stock == null)
            throw new KeyNotFoundException($"Stock with ID {id} not found");

        if (request.Name != null)
        {
            var existing = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Name == request.Name
                    && s.Warehouse_id == stock.Warehouse_id
                    && s.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Stock '{request.Name}' already exists in this warehouse");

            stock.Name = request.Name;
        }

        if (request.Quantity.HasValue)
            stock.Quantity = request.Quantity.Value;

        if (request.Reserved_quantity.HasValue)
        {
            if (request.Reserved_quantity.Value > stock.Quantity)
                throw new InvalidOperationException("Reserved quantity cannot exceed total quantity");
            stock.Reserved_quantity = request.Reserved_quantity.Value;
        }

        stock.Updated_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Stock {stock.Id} updated");
        return stock;
    }

    public async Task<bool> DeleteStockAsync(int id)
    {
        var stock = await _context.Stocks
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stock == null)
            return false;

        if (stock.Products.Any())
            throw new InvalidOperationException($"Cannot delete stock '{stock.Name}' because it contains products");

        _context.Stocks.Remove(stock);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Stock {id} deleted");
        return true;
    }

    #endregion

    #region Shipment Operations

    public async Task<Shippment> CreateShipmentAsync(CreateShippmentRequest request)
    {
        // Validate supplier exists
        var supplier = await _context.Suppliers.FindAsync(request.Supplier_id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with ID {request.Supplier_id} not found");

        // Validate warehouse exists
        var warehouse = await _context.Warehouses.FindAsync(request.Warehouse_id);
        if (warehouse == null)
            throw new KeyNotFoundException($"Warehouse with ID {request.Warehouse_id} not found");

        // Check if tracking number already exists
        var existingTracking = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Tracking_number == request.Tracking_number);

        if (existingTracking != null)
            throw new InvalidOperationException($"Shipment with tracking number '{request.Tracking_number}' already exists");

        // Generate shipment number
        var shipmentNumber = GenerateShipmentNumber();

        var shipment = new Shippment
        {
            Shipment_number = shipmentNumber,
            Supplier_id = request.Supplier_id,
            Warehouse_id = request.Warehouse_id,
            Expected_date = request.Expected_date,
            Tracking_number = request.Tracking_number,
            Shipping_fee = request.Shipping_fee,
            Notes = request.Notes,
            Shipment_status = ShippmentStatus.Created,
            Created_at = DateTime.UtcNow
        };

        // Add items
        foreach (var item in request.ShippingItems)
        {
            // Validate product exists
            var product = await _context.Products.FindAsync(item.Product_id);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {item.Product_id} not found");

            shipment.ShippingItems.Add(new ShippingItem
            {
                Product_id = item.Product_id,
                Quantity = item.Quantity,
                Unit_cost = item.Unit_cost
            });
        }

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Shipment {shipment.Shipment_number} created with ID {shipment.Id}");
        return shipment;
    }

    public async Task<Shippment> ReceiveShipmentAsync(int shipmentId, ReceiveShippmentRequest request)
    {
        var shipment = await _context.Shipments
            .Include(s => s.ShippingItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            throw new KeyNotFoundException($"Shipment with ID {shipmentId} not found");

        if (shipment.Shipment_status == ShippmentStatus.Received)
            throw new InvalidOperationException($"Shipment {shipment.Shipment_number} has already been received");

        if (shipment.Shipment_status == ShippmentStatus.Canceled)
            throw new InvalidOperationException($"Shipment {shipment.Shipment_number} has been canceled");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Update shipment status
            shipment.Shipment_status = ShippmentStatus.Received;
            shipment.Received_date = DateTime.UtcNow;
            shipment.Updated_at = DateTime.UtcNow;

            // Process each received item
            foreach (var receivedItem in request.Items)
            {
                var shipmentItem = shipment.ShippingItems
                    .FirstOrDefault(i => i.Product_id == receivedItem.Product_id);

                if (shipmentItem == null)
                    throw new InvalidOperationException($"Product {receivedItem.Product_id} not found in shipment");

                if (receivedItem.Quantity > shipmentItem.Quantity)
                    throw new InvalidOperationException(
                        $"Received quantity ({receivedItem.Quantity}) exceeds ordered quantity ({shipmentItem.Quantity}) for product {receivedItem.Product_id}");

                // Update product's stock
                var product = shipmentItem.Product;
                if (product.Stock_id.HasValue)
                {
                    var stock = await _context.Stocks.FindAsync(product.Stock_id.Value);
                    if (stock != null)
                    {
                        stock.Quantity += receivedItem.Quantity;
                        stock.Updated_at = DateTime.UtcNow;
                    }
                }
                else
                {
                    _logger.LogWarning($"Product {product.Id} has no assigned stock - quantity not updated");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Shipment {shipment.Shipment_number} received");
            return shipment;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Shippment> GetShipmentByIdAsync(int id)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Supplier)
            .Include(s => s.Warehouse)
            .Include(s => s.ShippingItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shipment == null)
            throw new KeyNotFoundException($"Shipment with ID {id} not found");

        return shipment;
    }

    public async Task<IEnumerable<Shippment>> GetAllShipmentsAsync()
    {
        return await _context.Shipments
            .Include(s => s.Supplier)
            .Include(s => s.Warehouse)
            .Include(s => s.ShippingItems)
            .ThenInclude(i => i.Product)
            .OrderByDescending(s => s.Created_at)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shippment>> GetShipmentsByWarehouseAsync(int warehouseId)
    {
        return await _context.Shipments
            .Include(s => s.Supplier)
            .Include(s => s.Warehouse)
            .Include(s => s.ShippingItems)
            .ThenInclude(i => i.Product)
            .Where(s => s.Warehouse_id == warehouseId)
            .OrderByDescending(s => s.Created_at)
            .ToListAsync();
    }

    #endregion

    #region Reserve and Ship Operations

    public async Task<bool> ReserveProductAsync(ReserveProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == request.Product_id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {request.Product_id} not found");

        if (!product.Stock_id.HasValue || product.Stock == null)
            throw new InvalidOperationException($"Product '{product.Name}' is not assigned to any stock");

        var stock = product.Stock;

        if (stock.AvailableQuantity < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock in '{stock.Name}'. Available: {stock.AvailableQuantity}, Requested: {request.Quantity}");

        stock.Reserved_quantity += request.Quantity;
        stock.Updated_at = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Reserved {request.Quantity} units of product '{product.Name}' from '{stock.Name}'");
        return true;
    }

    public async Task<bool> ShipProductAsync(ShipProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == request.Product_id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {request.Product_id} not found");

        if (!product.Stock_id.HasValue || product.Stock == null)
            throw new InvalidOperationException($"Product '{product.Name}' is not assigned to any stock");

        var stock = product.Stock;

        if (stock.Reserved_quantity < request.Quantity)
            throw new InvalidOperationException(
                $"Not enough reserved quantity in '{stock.Name}'. Reserved: {stock.Reserved_quantity}, Requested: {request.Quantity}");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            stock.Quantity -= request.Quantity;
            stock.Reserved_quantity -= request.Quantity;
            stock.Updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Shipped {request.Quantity} units of product '{product.Name}' in shipment {request.Shipment_number}");
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Supplier Operations

    public async Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request)
    {
        // Check unique constraints
        var existing = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Name == request.Name
                || s.Email == request.Email
                || s.Phone == request.Phone
                || s.Address == request.Address);

        if (existing != null)
            throw new InvalidOperationException("Supplier with the same name, email, phone, or address already exists");

        var supplier = new Supplier
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true,
            Created_at = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Supplier '{supplier.Name}' created with ID {supplier.Id}");
        return supplier;
    }

    public async Task<Supplier> GetSupplierByIdAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Shippments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with ID {id} not found");

        return supplier;
    }

    public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
    {
        return await _context.Suppliers
            .Include(s => s.Shippments)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Supplier> UpdateSupplierAsync(int id, UpdateSupplierRequest request)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with ID {id} not found");

        if (request.Name != null)
        {
            var existing = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Name == request.Name && s.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Supplier with name '{request.Name}' already exists");

            supplier.Name = request.Name;
        }

        if (request.Email != null)
        {
            var existing = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Email == request.Email && s.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Supplier with email '{request.Email}' already exists");

            supplier.Email = request.Email;
        }

        if (request.Phone != null)
        {
            var existing = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Phone == request.Phone && s.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Supplier with phone '{request.Phone}' already exists");

            supplier.Phone = request.Phone;
        }

        if (request.Address != null)
        {
            var existing = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Address == request.Address && s.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Supplier with address '{request.Address}' already exists");

            supplier.Address = request.Address;
        }

        if (request.IsActive.HasValue)
            supplier.IsActive = request.IsActive.Value;

        supplier.Updated_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Supplier {supplier.Id} updated");
        return supplier;
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Shippments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
            return false;

        if (supplier.Shippments.Any())
            throw new InvalidOperationException($"Cannot delete supplier '{supplier.Name}' because it has shipments");

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Supplier {id} deleted");
        return true;
    }

    #endregion

    #region Warehouse Operations

    public async Task<Models.Entities.Warehouse> CreateWarehouseAsync(CreateWarehouseRequest request)
    {
        var existing = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Name == request.Name || w.Email == request.Email);

        if (existing != null)
            throw new InvalidOperationException("Warehouse with the same name or email already exists");

        var warehouse = new Models.Entities.Warehouse
        {
            Name = request.Name,
            Country = request.Country,
            City = request.City,
            Email = request.Email,
            Capacity = request.Capacity,
            Created_at = DateTime.UtcNow
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Warehouse '{warehouse.Name}' created with ID {warehouse.Id}");
        return warehouse;
    }

    public async Task<Models.Entities.Warehouse> GetWarehouseByIdAsync(int id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Stocks)
            .Include(w => w.Shippments)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
            throw new KeyNotFoundException($"Warehouse with ID {id} not found");

        return warehouse;
    }

    public async Task<IEnumerable<Models.Entities.Warehouse>> GetAllWarehousesAsync()
    {
        return await _context.Warehouses
            .Include(w => w.Stocks)
            .Include(w => w.Shippments)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<Models.Entities.Warehouse> UpdateWarehouseAsync(int id, UpdateWarehouseRequest request)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null)
            throw new KeyNotFoundException($"Warehouse with ID {id} not found");

        if (request.Name != null)
        {
            var existing = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == request.Name && w.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Warehouse with name '{request.Name}' already exists");

            warehouse.Name = request.Name;
        }

        if (request.Country != null)
            warehouse.Country = request.Country;

        if (request.City != null)
            warehouse.City = request.City;

        if (request.Email != null)
        {
            var existing = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Email == request.Email && w.Id != id);

            if (existing != null)
                throw new InvalidOperationException($"Warehouse with email '{request.Email}' already exists");

            warehouse.Email = request.Email;
        }

        if (request.Capacity.HasValue)
            warehouse.Capacity = request.Capacity.Value;

        warehouse.Updated_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Warehouse {warehouse.Id} updated");
        return warehouse;
    }

    public async Task<bool> DeleteWarehouseAsync(int id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Stocks)
            .Include(w => w.Shippments)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
            return false;

        if (warehouse.Stocks.Any())
            throw new InvalidOperationException($"Cannot delete warehouse '{warehouse.Name}' because it has stocks");

        if (warehouse.Shippments.Any())
            throw new InvalidOperationException($"Cannot delete warehouse '{warehouse.Name}' because it has shipments");

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Warehouse {id} deleted");
        return true;
    }

    #endregion

    #region Helper Methods

    private string GenerateShipmentNumber()
    {
        var prefix = "SHP";
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999).ToString();
        return $"{prefix}-{date}-{random}";
    }

    #endregion
}