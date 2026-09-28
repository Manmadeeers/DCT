using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Contracts;
using Warehouse.Api.Exceptions;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/operations")]
public sealed class OperationsController(WarehouseService service) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost("{operation}")]
    public async Task<IActionResult> Execute(string operation, [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var name = operation.Replace("-", "").Replace("_", "").ToLowerInvariant();
        return name switch
        {
            "addproduct" => Ok(await service.AddProductAsync(Read<AddProductRequest>(payload), cancellationToken)),
            "receiveshipment" => Ok(await service.ReceiveShipmentAsync(Read<ReceiveShipmentRequest>(payload), cancellationToken)),
            "reserveproduct" => Ok(await service.ReserveProductAsync(Read<ReserveProductRequest>(payload), cancellationToken)),
            "shipproduct" => Ok(await service.ShipProductAsync(Read<ShipProductRequest>(payload), cancellationToken)),
            _ => throw new IncorrectOperationException($"Operation '{operation}' is not supported.")
        };
    }

    private static T Read<T>(JsonElement payload)
    {
        try
        {
            return payload.Deserialize<T>(JsonOptions)
                ?? throw new IncorrectInputDataException("Request body is missing.");
        }
        catch (JsonException)
        {
            throw new IncorrectInputDataException("Request JSON contains incorrect data.");
        }
    }
}
