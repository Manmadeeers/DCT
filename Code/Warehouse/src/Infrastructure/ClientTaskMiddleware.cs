using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Warehouse.Api.Infrastructure;

public sealed class LabOptions
{
    public int RequestDelaySeconds { get; set; }
    public int RaceDelaySeconds { get; set; } = 10;
    public int NetworkDelaySeconds { get; set; } = 0;
    public int TransactionFailStep { get; set; } = 0;
}

public sealed class RequestExecution
{
    public string ClientId { get; set; } = "";
    public string Operation { get; set; } = "";
    public string RequestId { get; set; } = "";
    public int TaskId { get; set; }

    public void Write(ILogger logger, string phase, string detail = "") =>
        logger.LogCritical(
            "[{Timestamp:HH:mm:ss.fff}] Client {ClientId} -> {Phase} " +
            "Operation={Operation} Task={TaskId} Thread={ThreadId} Request={RequestId} {Detail}",
            DateTimeOffset.Now, ClientId, phase, Operation, TaskId,
            Environment.CurrentManagedThreadId, RequestId, detail);
}

public sealed class ClientTaskMiddleware(
    RequestDelegate next,
    ILogger<ClientTaskMiddleware> logger,
    IOptions<LabOptions> options)
{
    public async Task InvokeAsync(HttpContext context, RequestExecution execution)
    {
        execution.ClientId = context.Request.Headers["X-Client-Id"].ToString();
        execution.Operation = $"{context.Request.Method} {context.Request.Path}";
        execution.RequestId = context.TraceIdentifier;

        await Task.Run(async () =>
        {
            execution.TaskId = Task.CurrentId ?? -1;
            execution.Write(logger, "START");
            try
            {
                context.RequestAborted.ThrowIfCancellationRequested();
                if (options.Value.NetworkDelaySeconds != 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(options.Value.NetworkDelaySeconds), context.RequestAborted);
                }
                if (context.Request.Path.StartsWithSegments("/api/operations") || context.Request.Path == "/api/operations/reserveproduct")
                {
                    await Task.Delay(TimeSpan.FromSeconds(options.Value.RequestDelaySeconds), context.RequestAborted);
                }
                await next(context);
            }
            catch (Exception ex) when (context.RequestAborted.IsCancellationRequested &&
                ex is OperationCanceledException or IOException or SocketException)
            {
                execution.Write(logger, "DISCONNECTED");
                if (!context.Response.HasStarted) context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled failure for client {ClientId}", execution.ClientId);
                if (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "Internal server error",
                        status = 500,
                        traceId = context.TraceIdentifier
                    }, context.RequestAborted);
                }
            }
            finally
            {
                if (context.RequestAborted.IsCancellationRequested)
                    execution.Write(logger, "CANCELLED");
                execution.Write(logger, "END", $"Status={context.Response.StatusCode}");
            }
        });
    }
}