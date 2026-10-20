using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Api.Contracts;
using Warehouse.Api.Data;
using Xunit;

namespace Warehouse.Api.Tests;

public sealed class RaceConditionTests(PostgresFixture postgres)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ConcurrentReservations_LoseOneUpdate()
    {
        using var barrier = new Barrier(2);
        using var logs = new ReservationLogs(barrier);

        await using var factory = new WarehouseApiFactory(
            postgres.Container.GetConnectionString(),
            raceDelaySeconds: 1,
            logs: logs);

        using var setup = factory.CreateClient();

        var bootstrap = await setup.GetFromJsonAsync<BootstrapResponse>(
            "/api/bootstrap");

        Assert.NotNull(bootstrap);

        using var added = await setup.PostAsJsonAsync(
            "/api/operations/AddProduct",
            new AddProductRequest(
                $"RACE-{Guid.NewGuid():N}",
                "Race test product"));

        added.EnsureSuccessStatusCode();

        var product =
            await added.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);

        using var shipment = await setup.PostAsJsonAsync(
            "/api/operations/ReceiveShipment",
            new ReceiveShipmentRequest(
                bootstrap.WarehouseId,
                bootstrap.StockId,
                bootstrap.SupplierId,
                new List<ShipmentItemRequest>
                {
                    new(product.Id, 1)
                }));

        shipment.EnsureSuccessStatusCode();

        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();

        firstClient.DefaultRequestHeaders.Add("X-Client-Id", "1");
        secondClient.DefaultRequestHeaders.Add("X-Client-Id", "2");

        var request = new ReserveProductRequest(
            bootstrap.WarehouseId,
            bootstrap.StockId,
            product.Id,
            1);

        var firstTask = firstClient.PostAsJsonAsync(
            "/api/operations/ReserveProduct",
            request);

        var secondTask = secondClient.PostAsJsonAsync(
            "/api/operations/ReserveProduct",
            request);

        await Task.WhenAll(firstTask, secondTask);

        using var firstResponse = await firstTask;
        using var secondResponse = await secondTask;

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        Assert.Equal(2, logs.Checks.Count);

        Assert.All(
            logs.Checks,
            message => Assert.Contains("Available=1", message));

        await using var scope = factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<WarehouseDbContext>();

        var item = await db.StockItems
            .AsNoTracking()
            .SingleAsync(x =>
                x.StockId == bootstrap.StockId &&
                x.ProductId == product.Id);

        Assert.Equal(1, item.Quantity);
        Assert.Equal(1, item.Reserved);
        Assert.Equal(0, item.Available);
    }

    private sealed class ReservationLogs(Barrier barrier)
        : ILoggerProvider
    {
        public ConcurrentQueue<string> Checks { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new ReservationLogger(this, barrier);
        }

        public void Dispose()
        {
        }

        private sealed class ReservationLogger(
            ReservationLogs owner,
            Barrier barrier) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (state is not
                    IEnumerable<KeyValuePair<string, object?>> values)
                {
                    return;
                }

                var fields = values.ToDictionary(
                    x => x.Key,
                    x => x.Value);

                if (!fields.TryGetValue("Phase", out var phase) ||
                    phase?.ToString() != "CHECK")
                {
                    return;
                }

                if (!fields.TryGetValue("ClientId", out var clientId) ||
                    clientId?.ToString() is not ("1" or "2"))
                {
                    return;
                }

                owner.Checks.Enqueue(formatter(state, exception));

                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(20)))
                {
                    throw new TimeoutException(
                        "Both reservations did not reach CHECK.");
                }
            }
        }
    }
}