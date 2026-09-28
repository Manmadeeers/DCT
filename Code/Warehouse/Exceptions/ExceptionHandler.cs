using System.Net.Sockets;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql;

namespace Warehouse.Exceptions;
public sealed class WarehouseExceptionHandler(ILogger<WarehouseExceptionHandler> logger) : IExceptionHandler
{

    public async ValueTask<bool>TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var result = MapException(exception);
        if (result.StatusCode >= 500)
        {
            logger.LogError(exception,"Request failed with code {StatusCode}",result.StatusCode);
        }
        else
        {
            logger.LogWarning(exception,"Request rejected with code {StatusCode}",result.StatusCode);
        }

        var details = new ProblemDetails
        {
          Status = result.StatusCode,
          Title = result.Message,
          Detail = result.Detail,
          Instance = context.Request.Path  
        };

        details.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = result.StatusCode;
        await context.Response.WriteAsJsonAsync(details,cancellationToken);
        return true;
    }

    private static ErrorResponse MapException(Exception exception)
    {
        return exception switch
        {
            UnavailableServerException=>new(StatusCodes.Status500InternalServerError,"Server unavailable",exception.Message),
            IncorrectOperationException=>new(StatusCodes.Status400BadRequest,"Incorrect operation",exception.Message),
            ObjectMissingException=>new(StatusCodes.Status404NotFound,"Object missing",exception.Message),
            IncorrectInputDataException=>new(StatusCodes.Status400BadRequest,"Incorect input data",exception.Message),
            NetworkException=>new(StatusCodes.Status503ServiceUnavailable,"Service unavailable",exception.Message),
            HttpRequestException=>new(StatusCodes.Status503ServiceUnavailable,"Service unavailable",exception.Message),
            SocketException=>new(StatusCodes.Status503ServiceUnavailable,"Service unavailable",exception.Message),
            TimeoutException=>new(StatusCodes.Status503ServiceUnavailable,"Service unavailable",exception.Message),
            PostgresException=>new(StatusCodes.Status500InternalServerError,"Internal service error",exception.Message),
            NpgsqlException=>new(StatusCodes.Status503ServiceUnavailable,"Service unavailable",exception.Message),
            _=>new(StatusCodes.Status500InternalServerError,"Internal server error","Unexpected server error ocured")
        };
    }
    
    private sealed record ErrorResponse(int StatusCode, string Message, string Detail);
}