using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/bootstrap")]
public sealed class BootstrapController(WarehouseService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await service.GetBootstrapAsync(ct));
}
