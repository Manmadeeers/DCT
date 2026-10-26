using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    .Validate(
        options =>
            options.RequestDelaySeconds is >= 0 and <= 60 &&
            options.RaceDelaySeconds is >= 0 and <= 60,
        "Lab delays must be between 0 and 60 seconds.")
    .ValidateOnStart();

builder.Services.AddSingleton<NpgsqlMultiHostDataSource>(services =>
{
    var configuration =
        services.GetRequiredService<IConfiguration>();

    var connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is required.");

    return new NpgsqlDataSourceBuilder(connectionString)
        .BuildMultiHost();
});

builder.Services.AddSingleton<NpgsqlDataSource>(services =>
    services.GetRequiredService<NpgsqlMultiHostDataSource>()
        .WithTargetSession(TargetSessionAttributes.ReadWrite));

builder.Services.AddDbContext<WarehouseDbContext>(
    (services, options) =>
        options.UseNpgsql(
            services.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton<DatabaseAvailabilityMonitor>();

builder.Services.AddHostedService<DatabaseAvailabilityMonitor>(
    services =>
        services.GetRequiredService<DatabaseAvailabilityMonitor>());

builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<DomainEventLogger>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "WarehouseClient",
        policy => policy
            .WithOrigins(
                "http://localhost:5500",
                "http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseMiddleware<ServerTimeMiddleware>();
app.UseMiddleware<ClientTaskMiddleware>();

app.UseCors("WarehouseClient");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapGet(
    "/health/databases",
    (DatabaseAvailabilityMonitor monitor) =>
        Results.Ok(monitor.Current));

app.MapGet(
    "/health",
    (DatabaseAvailabilityMonitor monitor) =>
    {
        var status = monitor.Current;

        return Results.Json(
            status,
            statusCode: status.Ready
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable);
    });

app.Run();

public partial class Program;