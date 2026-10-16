using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Infrastructure;
using Warehouse.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<RequestExecution>();
builder.Services.AddScoped<RaceReservationService>();
builder.Services.AddScoped<SyncReservationService>();
builder.Services.AddScoped<SemaphoreReservationService>();

builder.Services.AddOptions<LabOptions>()
    .Bind(builder.Configuration.GetSection("Lab"))
    .Validate(x => x.RequestDelaySeconds is >= 0 and <= 60 &&
                   x.RaceDelaySeconds is >= 0 and <= 60 && x.NetworkDelaySeconds is >= 0 and <= 60
                   && x.TransactionFailStep is >= 0 and <= 2,
        "Lab delays must be between 0 and 60 seconds. Transaction fail step must be between 0 and 2")
    .ValidateOnStart();

builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.")));

builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<DomainEventLogger>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WarehouseClient", policy => policy
        .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ServerTimeMiddleware>();
app.UseMiddleware<ClientTaskMiddleware>();

app.UseExceptionHandler();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("WarehouseClient");
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