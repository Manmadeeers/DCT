using System.Net.Sockets;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Warehouse.Api.Exceptions;

namespace Warehouse.Api.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);
        if (status >= 500) logger.LogError(exception, "Request failed: {Status}", status);
        else logger.LogWarning(exception, "Request rejected: {Status}", status);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status, Title = title, Detail = detail, Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail) Map(Exception ex) => ex switch
    {
        IncorrectOperationException => (400, "Incorrect operation", ex.Message),
        IncorrectInputDataException => (400, "Incorrect input data", ex.Message),
        ObjectMissingException => (404, "Object missing", ex.Message),
        UnavailableServerException => (503, "Server unavailable", ex.Message),
        NetworkException => (503, "Network error", ex.Message),
        HttpRequestException or SocketException => (503, "Network error", "A network request failed."),
        TimeoutException => (503, "Server unavailable", "The operation timed out."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
            => (400, "Incorrect input data", "A value already exists or an item was inserted concurrently."),
        PostgresException => (500, "Database error", "PostgreSQL rejected the operation."),
        NpgsqlException => (503, "Database unavailable", "The PostgreSQL server is unavailable."),
        DbUpdateException update when ContainsConnectionFailure(update)
            => (503, "Database unavailable", "The PostgreSQL server is unavailable."),
        DbUpdateException => (500, "Database update error", "The database operation failed."),
        _ => (500, "Internal server error", "An unexpected server error occurred.")
    };

    private static bool ContainsConnectionFailure(Exception ex)
    {
        for (var current = ex.InnerException; current is not null; current = current.InnerException)
            if (current is NpgsqlException and not PostgresException || current is SocketException)
                return true;
        return false;
    }
}
