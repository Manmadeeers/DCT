using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Warehouse.Api.Tests;

public sealed class WarehouseApiFactory(string connectionString,
    int requestDelaySeconds = 0, int raceDelaySeconds = 0,
    ILoggerProvider? logs = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (logs is not null) builder.ConfigureLogging(logging => logging.AddProvider(logs));
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Lab:RequestDelaySeconds"] = requestDelaySeconds.ToString(),
                ["Lab:RaceDelaySeconds"] = raceDelaySeconds.ToString()
            }));
    }
}