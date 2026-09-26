using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Data;
using Warehouse.Services;
using Warehouse.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
