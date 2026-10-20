using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Api.Contracts;
using Warehouse.Api.Data;
using Warehouse.Api.Domain;
using Warehouse.Api.Infrastructure;
using Warehouse.Api.Services;
using Xunit;

namespace Warehouse.Api.Tests;

public sealed class TransactionTests(PostgresFixture postgres)
    : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task ArtificialFailure_RollsBackAllChanges(
        int failStep,
        bool existingStock)
    {
        await using var baseFactory = new WarehouseApiFactory(
            postgres.Container.GetConnectionString());

        await using var factory = ConfigureFailure(
            baseFactory,
            failStep);

        var scenario = await PrepareAsync(factory, existingStock);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<WarehouseService>();

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () =>
                    {
                        await service.ReceiveShipmentAsync(
                            scenario.Request,
                            CancellationToken.None);
                    });

            var expectedPrefix = failStep == 1
                ? "Artificial failure after saving the shipment."
                : "Artificial failure after saving shipment items.";

            Assert.StartsWith(expectedPrefix, exception.Message);
        }

        await using var verification =
            factory.Services.CreateAsyncScope();

        var db = verification.ServiceProvider
            .GetRequiredService<WarehouseDbContext>();

        var shipments = await db.Shipments
            .AsNoTracking()
            .Where(x => x.SupplierId == scenario.Request.SupplierId)
            .ToListAsync();

        Assert.Empty(shipments);

        var shipmentItems = await db.ShipmentItems
            .AsNoTracking()
            .Where(x => x.ProductId == scenario.ProductId)
            .ToListAsync();

        Assert.Empty(shipmentItems);

        var stockItem = await db.StockItems
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.StockId == scenario.Request.StockId &&
                x.ProductId == scenario.ProductId);

        if (existingStock)
        {
            Assert.NotNull(stockItem);
            Assert.Equal(10, stockItem.Quantity);
            Assert.Equal(2, stockItem.Reserved);
            Assert.Equal(8, stockItem.Available);
        }
        else
        {
            Assert.Null(stockItem);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulShipment_CommitsAllChanges(
        bool existingStock)
    {
        await using var baseFactory = new WarehouseApiFactory(
            postgres.Container.GetConnectionString());

        await using var factory = ConfigureFailure(
            baseFactory,
            failStep: 0);

        var scenario = await PrepareAsync(factory, existingStock);

        ShipmentResponse result;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<WarehouseService>();

            result = await service.ReceiveShipmentAsync(
                scenario.Request,
                CancellationToken.None);
        }

        await using var verification =
            factory.Services.CreateAsyncScope();

        var db = verification.ServiceProvider
            .GetRequiredService<WarehouseDbContext>();

        var shipment = await db.Shipments
            .AsNoTracking()
            .SingleAsync(x =>
                x.SupplierId == scenario.Request.SupplierId);

        Assert.Equal(result.Id, shipment.Id);
        Assert.Equal(scenario.Request.WarehouseId, shipment.WarehouseId);
        Assert.Equal(scenario.Request.StockId, shipment.StockId);
        Assert.Equal(scenario.Request.SupplierId, shipment.SupplierId);

        var shipmentItems = await db.ShipmentItems
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipment.Id)
            .ToListAsync();

        var shipmentItem = Assert.Single(shipmentItems);

        Assert.Equal(scenario.ProductId, shipmentItem.ProductId);
        Assert.Equal(3, shipmentItem.Quantity);

        var stockItem = await db.StockItems
            .AsNoTracking()
            .SingleAsync(x =>
                x.StockId == scenario.Request.StockId &&
                x.ProductId == scenario.ProductId);

        Assert.Equal(existingStock ? 13 : 3, stockItem.Quantity);
        Assert.Equal(existingStock ? 2 : 0, stockItem.Reserved);
        Assert.Equal(existingStock ? 11 : 3, stockItem.Available);
    }

    private static WebApplicationFactory<Program> ConfigureFailure(
        WarehouseApiFactory factory,
        int failStep)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<LabOptions>(options =>
                {
                    options.TransactionFailStep = failStep;
                });
            });
        });
    }

    private static async Task<Scenario> PrepareAsync(
        WebApplicationFactory<Program> factory,
        bool existingStock)
    {
        using var client = factory.CreateClient();

        var bootstrap = await client
            .GetFromJsonAsync<BootstrapResponse>("/api/bootstrap");

        Assert.NotNull(bootstrap);

        await using var scope = factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<WarehouseDbContext>();

        var product = new Product
        {
            Sku = $"TX-{Guid.NewGuid():N}",
            Name = "Transaction test product"
        };

        var supplier = new Suplier
        {
            Name = $"TX-{Guid.NewGuid():N}"
        };

        db.Products.Add(product);
        db.Supliers.Add(supplier);

        await db.SaveChangesAsync();

        if (existingStock)
        {
            db.StockItems.Add(new StockItem
            {
                StockId = bootstrap.StockId,
                ProductId = product.Id,
                Quantity = 10,
                Reserved = 2
            });

            await db.SaveChangesAsync();
        }

        var request = new ReceiveShipmentRequest(
            bootstrap.WarehouseId,
            bootstrap.StockId,
            supplier.Id,
            new List<ShipmentItemRequest>
            {
                new(product.Id, 3)
            });

        return new Scenario(request, product.Id);
    }

    private sealed record Scenario(
        ReceiveShipmentRequest Request,
        int ProductId);
}