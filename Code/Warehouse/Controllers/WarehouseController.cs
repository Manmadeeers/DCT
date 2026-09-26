using Microsoft.AspNetCore.Mvc;
using Warehouse.Models.DTOs;
using Warehouse.Services;

namespace Warehouse.Controllers
{
    [ApiController]
    [Route("/api/warehouse")]
    public class WarehouseController : ControllerBase
    {
        private readonly WarehouseService _service;
        public WarehouseController(WarehouseService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAllWarehouses(CancellationToken ct)
        {
            var result = await _service.GetAllWarehousesAsync(ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetWarehouseById(int id, CancellationToken ct)
        {
            var result = await _service.GetWarehouseByIdAsync(id, ct);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddWarehouse(CreateWarehouseRequest body, CancellationToken ct)
        {
            try
            {
                var result = await _service.AddWarehouseAsync(body, ct);
                return CreatedAtAction(nameof(AddWarehouse),result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteWarehouse(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteWarehouseAsync(id,ct);
                return NoContent();
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new {error=ex.Message});
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new {error=ex.Message});
            }
        }
    }
}