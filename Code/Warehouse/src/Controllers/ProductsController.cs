using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Contracts;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    WarehouseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ProductResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await service.GetProductsAsync(cancellationToken);

        return Ok(products);
    }
}