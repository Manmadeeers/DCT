using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using Warehouse.Api.Contracts;
using Xunit.Abstractions;

namespace Warehouse.Api.Tests;

// HTTP integration tests: real service and PostgreSQL, in-process TestServer.
public sealed class Lab2Tests(PostgresFixture postgres, ITestOutputHelper output)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task OneClient_ReservesProduct()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = Client(factory, 1);
        var request = await Prepare(client, 3);
        using var response = await client.PostAsJsonAsync("/api/operations/ReserveProduct", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await ReadItem(client, request);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(1, item.Reserved);
        Assert.Equal(2, item.Available);
    }

    [Fact]
    public async Task TwoClients_BothReceiveResponses()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var first = Client(factory, 1);
        using var second = Client(factory, 2);
        var a = AddProduct(first);
        var b = AddProduct(second);
        var responses = await Task.WhenAll(a, b);
        var ids = new List<int>();
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
                Assert.NotNull(product);
                ids.Add(product.Id);
            }
        }
        Assert.Equal(2, ids.Distinct().Count());
        var products = await first.GetFromJsonAsync<ProductResponse[]>("/api/products");
        Assert.NotNull(products);
        Assert.All(ids, id => Assert.Contains(products, product => product.Id == id));
    }

    [Fact]
    public async Task ParallelProcessing_SecondStartsBeforeFirstEnds()
    {
        using var logs = new CapturedLogs();
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString(),
            requestDelaySeconds: 10, logs: logs);
        using var first = Client(factory, 1);
        using var second = Client(factory, 2);
        var a = AddProduct(first);
        var start1 = await logs.Wait("1", "START");
        var b = AddProduct(second);
        var start2 = await logs.Wait("2", "START");
        using var response1 = await a;
        using var response2 = await b;
        var end1 = await logs.Wait("1", "END");
        var end2 = await logs.Wait("2", "END");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.True(start1.Time <= start2.Time && start2.Time < end1.Time);
        Assert.True(end1.Time - start1.Time >= TimeSpan.FromSeconds(9.5));
        Assert.True(end2.Time - start2.Time >= TimeSpan.FromSeconds(9.5));
        Assert.True(start1.TaskId > 0 && start2.TaskId > 0);
        Assert.NotEqual(start1.TaskId, start2.TaskId);
        foreach (var entry in logs.Entries) output.WriteLine(entry.Message);
    }

    [Fact]
    public async Task RaceCondition_TwoSuccessesButOnlyOneReservationStored()
    {
        using var logs = new CapturedLogs();
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString(),
            raceDelaySeconds: 10, logs: logs);
        using var setup = Client(factory, 99);
        var request = await Prepare(setup, 1);
        using var first = Client(factory, 1);
        using var second = Client(factory, 2);
        var a = first.PostAsJsonAsync("/api/operations/ReserveProduct", request);
        var check1 = await logs.Wait("1", "CHECK");
        var b = second.PostAsJsonAsync("/api/operations/ReserveProduct", request);
        var check2 = await logs.Wait("2", "CHECK");
        using var response1 = await a;
        using var response2 = await b;
        var write1 = await logs.Wait("1", "WRITE");
        var write2 = await logs.Wait("2", "WRITE");
        Assert.True(check2.Time < write1.Time && check1.Time < write2.Time);
        Assert.Contains("Available=1", check1.Message);
        Assert.Contains("Available=1", check2.Message);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var item = await ReadItem(setup, request);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(1, item.Reserved); // Two accepted requests, one stored increment.
        Assert.Equal(0, item.Available);
        output.WriteLine("Accepted reservations: 2; Quantity=1; Reserved=1; Available=0.");
        foreach (var entry in logs.Entries) output.WriteLine(entry.Message);
    }

    [Fact]
    public async Task DisconnectedClient_DoesNotStopServer()
    {
        using var logs = new CapturedLogs();
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString(),
            requestDelaySeconds: 10, logs: logs);
        using var client = Client(factory, 1);
        using var cancellation = new CancellationTokenSource();
        var pending = client.PostAsJsonAsync("/api/operations/AddProduct",
            new AddProductRequest($"P-{Guid.NewGuid():N}", "Cancelled"), cancellation.Token);
        await logs.Wait("1", "START");
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await pending; });
        await logs.Wait("1", "CANCELLED");
        await logs.Wait("1", "END");
        using var healthy = Client(factory, 2);
        using var response = await healthy.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestAndMissingResource_DoNotStopServer()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = Client(factory, 1);
        using var malformed = await client.PostAsync("/api/operations/AddProduct",
            new StringContent("{", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        using var unknown = await client.PostAsJsonAsync("/api/operations/Unknown", new { });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        using var missing = await client.GetAsync($"/api/stocks/{int.MaxValue}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var healthy = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
    }

    [Fact]
    public async Task ProcessingError_Returns500AndServerContinues()
    {
        await using var factory = new WarehouseApiFactory(postgres.Container.GetConnectionString());
        using var client = Client(factory, 1);
        var request = await Prepare(client, 1);
        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();
        // The supplied schema has no unique key on stockitem.
        // A duplicate causes SingleOrDefaultAsync to throw during processing.
        await using var insert = new NpgsqlCommand(
            "INSERT INTO stockitem (stockid, productid, quantity, reserved) VALUES (@s, @p, 1, 0)",
            connection);
        insert.Parameters.AddWithValue("s", request.StockId);
        insert.Parameters.AddWithValue("p", request.ProductId);
        await insert.ExecuteNonQueryAsync();
        try
        {
            using var failed = await client.PostAsJsonAsync("/api/operations/ReserveProduct", request);
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            using var healthy = await client.GetAsync("/api/products");
            Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand(
                "DELETE FROM stockitem WHERE stockid=@s AND productid=@p", connection);
            cleanup.Parameters.AddWithValue("s", request.StockId);
            cleanup.Parameters.AddWithValue("p", request.ProductId);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static HttpClient Client(WarehouseApiFactory factory, int id)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", id.ToString());
        return client;
    }

    private static Task<HttpResponseMessage> AddProduct(HttpClient client) =>
        client.PostAsJsonAsync("/api/operations/AddProduct",
            new AddProductRequest($"P-{Guid.NewGuid():N}", "Lab2 product"));

    private static async Task<ReserveProductRequest> Prepare(HttpClient client, int quantity)
    {
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/bootstrap");
        Assert.NotNull(bootstrap);
        using var added = await AddProduct(client);
        added.EnsureSuccessStatusCode();
        var product = await added.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        using var shipment = await client.PostAsJsonAsync("/api/operations/ReceiveShipment",
            new ReceiveShipmentRequest(bootstrap.WarehouseId, bootstrap.StockId, bootstrap.SupplierId,
                new List<ShipmentItemRequest> { new(product.Id, quantity) }));
        shipment.EnsureSuccessStatusCode();
        return new ReserveProductRequest(bootstrap.WarehouseId, bootstrap.StockId, product.Id, 1);
    }

    private static async Task<StockItemResponse> ReadItem(HttpClient client, ReserveProductRequest request)
    {
        var stock = await client.GetFromJsonAsync<StockResponse>($"/api/stocks/{request.StockId}");
        Assert.NotNull(stock);
        return Assert.Single(stock.Items, x => x.ProductId == request.ProductId);
    }

    private sealed record Entry(string Client, string Phase, DateTimeOffset Time, int TaskId, string Message);

    private sealed class CapturedLogs : ILoggerProvider
    {
        public ConcurrentQueue<Entry> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Capture(Entries);
        public void Dispose() { }

        public async Task<Entry> Wait(string client, string phase)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (true)
            {
                var entry = Entries.FirstOrDefault(x => x.Client == client && x.Phase == phase);
                if (entry is not null) return entry;
                await Task.Delay(20, timeout.Token);
            }
        }

        private sealed class Capture(ConcurrentQueue<Entry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (state is not IEnumerable<KeyValuePair<string, object?>> values) return;
                var fields = values.ToDictionary(x => x.Key, x => x.Value);
                if (!fields.ContainsKey("Phase")) return;
                entries.Enqueue(new Entry((string)fields["ClientId"]!, (string)fields["Phase"]!,
                    (DateTimeOffset)fields["Timestamp"]!, (int)fields["TaskId"]!,
                    formatter(state, exception)));
            }
        }
    }
}