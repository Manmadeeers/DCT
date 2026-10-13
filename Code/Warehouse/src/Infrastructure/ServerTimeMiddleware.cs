using System.Globalization;

namespace Warehouse.Api.Infrastructure;

public sealed class ServerTimeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Server-Time"] =
                DateTimeOffset.Now.ToString(
                    "O",
                    CultureInfo.InvariantCulture);

            return Task.CompletedTask;
        });

        await next(context);
    }
}