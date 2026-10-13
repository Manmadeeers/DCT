using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client.Clock;
using Client.API;
using Client.ConsoleApp;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length < 3 ||
            !int.TryParse(args[0], out var id) ||
            id <= 0 ||
            !Uri.TryCreate(args[1], UriKind.Absolute, out var server) ||
            (server.Scheme != "http" && server.Scheme != "https")
            || !int.TryParse(args[2], out var offset))
        {
            Console.WriteLine(
                "Usage: dotnet run -- <client-id> <server-url> <clock-offset");
            return;
        }

        var clock = new ClientClock(TimeSpan.FromSeconds(offset));
        using var client = new WarehouseApiClient(id, server, clock);
        var application = new ConsoleApplication(id, client, clock);

        await application.RunAsync();
    }
}


