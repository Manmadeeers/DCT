using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Services;
using Warehouse.Api.Contracts;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/time")]
public sealed class TimeController(
    WarehouseService service
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ServerTimeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetServerTime(CancellationToken ct)
    {
        var serverTime = await service.GetServerTimeAsync(ct);
        return Ok(serverTime);
    }
}
