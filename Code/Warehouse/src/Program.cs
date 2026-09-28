using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Infrastructure;
using Warehouse.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:WarehouseDb is required.");

builder.Services.AddControllers();
builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<DomainEventLogger>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();
app.MapGet("/health", async (WarehouseDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "Healthy", database = "Connected" })
            : Results.Json(new { status = "Unhealthy", database = "Disconnected" }, statusCode: 503);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        return Results.Json(new { status = "Unhealthy", database = "Unavailable" }, statusCode: 503);
    }
});
app.Run();

public partial class Program;
