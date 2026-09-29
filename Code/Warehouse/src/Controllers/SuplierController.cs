using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Contracts;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(
    WarehouseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<SupplierResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SupplierResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var suppliers = await service.GetSuppliersAsync(
            cancellationToken);

        return Ok(suppliers);
    }
}