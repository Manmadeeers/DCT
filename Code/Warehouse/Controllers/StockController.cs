using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Models.DTOs;
using Warehouse.Services;

namespace Warehouse.Controllers
{   
    [ApiController]
    [Route("/api/stocks")]
    public class StockController : ControllerBase
    {
        private readonly StockService _service;
        public StockController(StockService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAllStocks(CancellationToken ct)
        {
            var stocks = await _service.GetAllStocksAsync(ct);
            return Ok(stocks);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStockById(int id, CancellationToken ct)
        {
            var stock = await _service.GetStockByIdAsync(id, ct);
            return stock is null ? NotFound() : Ok(stock);
        }

        [HttpPost]
        public async Task<IActionResult> AddStock([FromBody]CreateStockRequest request, CancellationToken ct)
        {
            try
            {
                var created = await _service.AddStockAsync(request, ct);
                return CreatedAtAction(nameof(AddStock), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody]UpdateStockRequest request, CancellationToken ct)
        {
            try
            {
                await _service.UpdateStockAsync(id, request, ct);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStock(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteStockAsync(id,ct);
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

        [HttpPost("/reserve")]
        public async Task<IActionResult> ReserveProduct([FromBody]ReserveProductRequest request, CancellationToken ct)
        {
            try
            {
                var reserved = await _service.ReserveProductAsync(request,ct);
                return Ok(reserved);
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new {error=ex.Message});
            }
            catch(ArgumentException ex)
            {
                return BadRequest(new {error=ex.Message});
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new {error=ex.Message});
            }
        }
    }
}