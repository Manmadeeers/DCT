using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Data;
using Warehouse.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
