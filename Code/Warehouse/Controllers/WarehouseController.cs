using Microsoft.AspNetCore.Mvc;
using Warehouse.Models.DTOs;
using Warehouse.Services.Interfaces;

namespace Warehouse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly ILogger<WarehouseController> _logger;

    public WarehouseController(IWarehouseService warehouseService, ILogger<WarehouseController> logger)
    {
        _warehouseService = warehouseService;
        _logger = logger;
    }

    #region Product Endpoints

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var product = await _warehouseService.AddProductAsync(request);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { error = "An error occurred while creating the product" });
        }
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        try
        {
            var product = await _warehouseService.GetProductByIdAsync(id);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting product {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the product" });
        }
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetAllProducts()
    {
        try
        {
            var products = await _warehouseService.GetAllProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            return StatusCode(500, new { error = "An error occurred while retrieving products" });
        }
    }

    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var product = await _warehouseService.UpdateProductAsync(id, request);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating product {id}");
            return StatusCode(500, new { error = "An error occurred while updating the product" });
        }
    }

    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var result = await _warehouseService.DeleteProductAsync(id);
            if (!result)
                return NotFound(new { error = $"Product with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting product {id}");
            return StatusCode(500, new { error = "An error occurred while deleting the product" });
        }
    }

    #endregion

    #region Stock Endpoints

    [HttpPost("stocks")]
    public async Task<IActionResult> CreateStock([FromBody] CreateStockRequest request)
    {
        try
        {
            var stock = await _warehouseService.CreateStockAsync(request);
            return CreatedAtAction(nameof(GetStock), new { id = stock.Id }, stock);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock");
            return StatusCode(500, new { error = "An error occurred while creating the stock" });
        }
    }

    [HttpGet("stocks/{id}")]
    public async Task<IActionResult> GetStock(int id)
    {
        try
        {
            var stock = await _warehouseService.GetStockByIdAsync(id);
            return Ok(stock);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting stock {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the stock" });
        }
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> GetAllStocks()
    {
        try
        {
            var stocks = await _warehouseService.GetAllStocksAsync();
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stocks");
            return StatusCode(500, new { error = "An error occurred while retrieving stocks" });
        }
    }

    [HttpGet("warehouses/{warehouseId}/stocks")]
    public async Task<IActionResult> GetStocksByWarehouse(int warehouseId)
    {
        try
        {
            var stocks = await _warehouseService.GetStocksByWarehouseAsync(warehouseId);
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting stocks for warehouse {warehouseId}");
            return StatusCode(500, new { error = "An error occurred while retrieving stocks" });
        }
    }

    [HttpPut("stocks/{id}")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            var stock = await _warehouseService.UpdateStockAsync(id, request);
            return Ok(stock);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating stock {id}");
            return StatusCode(500, new { error = "An error occurred while updating the stock" });
        }
    }

    [HttpDelete("stocks/{id}")]
    public async Task<IActionResult> DeleteStock(int id)
    {
        try
        {
            var result = await _warehouseService.DeleteStockAsync(id);
            if (!result)
                return NotFound(new { error = $"Stock with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting stock {id}");
            return StatusCode(500, new { error = "An error occurred while deleting the stock" });
        }
    }

    #endregion

    #region Shipment Endpoints

    [HttpPost("shipments")]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShippmentRequest request)
    {
        try
        {
            var shipment = await _warehouseService.CreateShippmentAsync(request);
            return CreatedAtAction(nameof(GetShipment), new { id = shipment.Id }, shipment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment");
            return StatusCode(500, new { error = "An error occurred while creating the shipment" });
        }
    }

    [HttpPost("shipments/{id}/receive")]
    public async Task<IActionResult> ReceiveShipment(int id, [FromBody] ReceiveShippmentRequest request)
    {
        try
        {
            var shipment = await _warehouseService.ReceiveShippmentAsync(id, request);
            return Ok(shipment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error receiving shipment {id}");
            return StatusCode(500, new { error = "An error occurred while receiving the shipment" });
        }
    }

    [HttpGet("shipments/{id}")]
    public async Task<IActionResult> GetShipment(int id)
    {
        try
        {
            var shipment = await _warehouseService.GetShippmentByIdAsync(id);
            return Ok(shipment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting shipment {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the shipment" });
        }
    }

    [HttpGet("shipments")]
    public async Task<IActionResult> GetAllShipments()
    {
        try
        {
            var shipments = await _warehouseService.GetAllShippmentsAsync();
            return Ok(shipments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all shipments");
            return StatusCode(500, new { error = "An error occurred while retrieving shipments" });
        }
    }

    [HttpGet("warehouses/{warehouseId}/shipments")]
    public async Task<IActionResult> GetShipmentsByWarehouse(int warehouseId)
    {
        try
        {
            var shipments = await _warehouseService.GetShippmentsByWarehouseAsync(warehouseId);
            return Ok(shipments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting shipments for warehouse {warehouseId}");
            return StatusCode(500, new { error = "An error occurred while retrieving shipments" });
        }
    }

    #endregion

    #region Reserve and Ship Endpoints

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveProduct([FromBody] ReserveProductRequest request)
    {
        try
        {
            var result = await _warehouseService.ReserveProductAsync(request);
            return Ok(new
            {
                success = result,
                message = $"Reserved {request.Quantity} units of product {request.Product_id}"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving product");
            return StatusCode(500, new { error = "An error occurred while reserving the product" });
        }
    }

    [HttpPost("ship")]
    public async Task<IActionResult> ShipProduct([FromBody] ShipProductRequest request)
    {
        try
        {
            var result = await _warehouseService.ShipProductAsync(request);
            return Ok(new
            {
                success = result,
                message = $"Shipped {request.Quantity} units of product {request.Product_id} in shipment {request.Shipment_number}"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping product");
            return StatusCode(500, new { error = "An error occurred while shipping the product" });
        }
    }

    #endregion

    #region Supplier Endpoints

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        try
        {
            var supplier = await _warehouseService.CreateSupplierAsync(request);
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return StatusCode(500, new { error = "An error occurred while creating the supplier" });
        }
    }

    [HttpGet("suppliers/{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        try
        {
            var supplier = await _warehouseService.GetSupplierByIdAsync(id);
            return Ok(supplier);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting supplier {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the supplier" });
        }
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetAllSuppliers()
    {
        try
        {
            var suppliers = await _warehouseService.GetAllSuppliersAsync();
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all suppliers");
            return StatusCode(500, new { error = "An error occurred while retrieving suppliers" });
        }
    }

    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
    {
        try
        {
            var supplier = await _warehouseService.UpdateSupplierAsync(id, request);
            return Ok(supplier);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating supplier {id}");
            return StatusCode(500, new { error = "An error occurred while updating the supplier" });
        }
    }

    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        try
        {
            var result = await _warehouseService.DeleteSupplierAsync(id);
            if (!result)
                return NotFound(new { error = $"Supplier with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting supplier {id}");
            return StatusCode(500, new { error = "An error occurred while deleting the supplier" });
        }
    }

    #endregion

    #region Warehouse Endpoints

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        try
        {
            var warehouse = await _warehouseService.CreateWarehouseAsync(request);
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            return StatusCode(500, new { error = "An error occurred while creating the warehouse" });
        }
    }

    [HttpGet("warehouses/{id}")]
    public async Task<IActionResult> GetWarehouse(int id)
    {
        try
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            return Ok(warehouse);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting warehouse {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the warehouse" });
        }
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetAllWarehouses()
    {
        try
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            return Ok(warehouses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all warehouses");
            return StatusCode(500, new { error = "An error occurred while retrieving warehouses" });
        }
    }

    [HttpPut("warehouses/{id}")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseRequest request)
    {
        try
        {
            var warehouse = await _warehouseService.UpdateWarehouseAsync(id, request);
            return Ok(warehouse);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating warehouse {id}");
            return StatusCode(500, new { error = "An error occurred while updating the warehouse" });
        }
    }

    [HttpDelete("warehouses/{id}")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        try
        {
            var result = await _warehouseService.DeleteWarehouseAsync(id);
            if (!result)
                return NotFound(new { error = $"Warehouse with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting warehouse {id}");
            return StatusCode(500, new { error = "An error occurred while deleting the warehouse" });
        }
    }

    #endregion
}