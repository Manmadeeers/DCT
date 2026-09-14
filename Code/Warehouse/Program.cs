using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.Run();
