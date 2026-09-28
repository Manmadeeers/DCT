using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Contracts;

namespace Warehouse.Api.Tests;

public sealed class OperationsTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task SuccessfulScenario_UsesGeneratedIntegerIdsAndPersistsStock()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = factory.CreateClient();
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/bootstrap");
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.WarehouseId > 0 && bootstrap.StockId > 0 && bootstrap.SupplierId > 0);

        var added = await client.PostAsJsonAsync("/api/operations/AddProduct",
            new AddProductRequest($"P-{Guid.NewGuid():N}", "Laptop"));
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var product = await added.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.True(product.Id > 0);

        var received = await client.PostAsJsonAsync("/api/operations/ReceiveShipment",
            new ReceiveShipmentRequest(bootstrap.WarehouseId, bootstrap.StockId, bootstrap.SupplierId,
                new List<ShipmentItemRequest> { new(product.Id, 10) }));
        Assert.Equal(HttpStatusCode.OK, received.StatusCode);
        var shipment = await received.Content.ReadFromJsonAsync<ShipmentResponse>();
        Assert.NotNull(shipment);
        Assert.True(shipment.Id > 0);

        var reserved = await client.PostAsJsonAsync("/api/operations/ReserveProduct",
            new ReserveProductRequest(bootstrap.WarehouseId, bootstrap.StockId, product.Id, 3));
        Assert.Equal(HttpStatusCode.OK, reserved.StatusCode);
        var reservedStock = await reserved.Content.ReadFromJsonAsync<StockResponse>();
        Assert.NotNull(reservedStock);
        var reservedItem = Assert.Single(reservedStock.Items, x => x.ProductId == product.Id);
        Assert.Equal(10, reservedItem.Quantity);
        Assert.Equal(3, reservedItem.Reserved);
        Assert.Equal(7, reservedItem.Available);

        var shipped = await client.PostAsJsonAsync("/api/operations/ShipProduct",
            new ShipProductRequest(bootstrap.WarehouseId, bootstrap.StockId, product.Id, 3));
        Assert.Equal(HttpStatusCode.OK, shipped.StatusCode);
        var stock = await client.GetFromJsonAsync<StockResponse>($"/api/stocks/{bootstrap.StockId}");
        Assert.NotNull(stock);
        var item = Assert.Single(stock.Items, x => x.ProductId == product.Id);
        Assert.Equal(7, item.Quantity);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(7, item.Available);
    }

    [Fact]
    public async Task UnknownOperation_Returns400()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/DeleteUniverse", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Incorrect operation", (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }

    [Fact]
    public async Task IncorrectData_Returns400()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/AddProduct",
            new AddProductRequest("", "Invalid Product"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Incorrect input data", (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }

    [Fact]
    public async Task DatabaseUnavailable_Returns503()
    {
        const string unavailable = "Host=127.0.0.1;Port=1;Database=warehouse;Username=invalid;Password=invalid;Timeout=1;Command Timeout=1";
        await using var factory = new WarehouseApiFactory(unavailable);
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/AddProduct",
            new AddProductRequest($"P-{Guid.NewGuid():N}", "Monitor"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Database unavailable", (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }
}
