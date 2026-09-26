using Microsoft.AspNetCore.Mvc;
using Warehouse.Models.DTOs;
using Warehouse.Services;

namespace Warehouse.Controllers
{   [ApiController]
    [Route("/api/suppliers")]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierService _service;
        public SupplierController(SupplierService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAllSuppliers(CancellationToken ct)
        {
            var result = await _service.GetAllSuppliersAsync(ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSupplierById(int id, CancellationToken ct)
        {
            var result = await _service.GetSupplierByIdAsync(id, ct);
            if (result is not null)
            {
                return Ok(result);
            }
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> AddSupplier([FromBody] CreateSupplierRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.AddSupplierAsync(request, ct);
                return CreatedAtAction(nameof(AddSupplier), result);
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
        {
            try
            {
                await _service.UpdateSupplierAsync(id, request, ct);
                return NoContent();
            }
            catch (InvalidCastException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }

        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSupplierAsync(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteSupplierAsync(id, ct);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("shippment")]
        public async Task<IActionResult> CreateShippment(CreateShippmentRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.CreateShippmentAsync(request, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("item")]
        public async Task<IActionResult> AddItem(CreateShippingItemRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.AddShippingItemAsync(request, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}