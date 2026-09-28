using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(WarehouseService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) =>
        Ok(await service.GetStockAsync(id, ct));
}
