namespace Client.ConsoleApp;

using Client.API;
using Client.Clock;

public sealed class ConsoleApplication
{
    private readonly int _clientId;
    private readonly ClientClock _clock;
    private readonly WarehouseApiClient _client;
    public ConsoleApplication(
        int clientId,
        WarehouseApiClient client,
        ClientClock clock)
    {
        _clientId = clientId;
        _client = client;
        _clock = clock;
    }

    public DateTime localTime { get; set; }

    public async Task RunAsync()
    {
        using var stop = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            Console.WriteLine(
                $"Client {_clientId}. Enter: METHOD /api/path. " +
                "Empty line exits. Ctrl+C cancels.");

            while (!stop.IsCancellationRequested)
            {
                Console.Write("> ");
                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                if (!TryParseCommand(line, out var method, out var path))
                {
                    Console.WriteLine(
                        "Enter GET or POST and a path starting with /.");
                    continue;
                }

                string? json = null;

                if (method == "POST")
                {
                    Console.Write("JSON: ");
                    json = Console.ReadLine() ?? "";
                }

                try
                {
                    Log("-> SEND");

                    var response = await _client.SendAsync(
                        method,
                        path,
                        json,
                        stop.Token);

                    Log($"<- HTTP {response.StatusCode}");
                    Console.WriteLine(response.Body);

                    PrintTimeStamps();

                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine(
                        $"Server unavailable or connection lost: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(
                        stop.IsCancellationRequested
                            ? "Request cancelled."
                            : "Server response timed out.");
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private void PrintTimeStamps()
    {
        Console.WriteLine($"Client local time: {_clock.LocalTime:O}");
        if (_clock.ServerTime is DateTimeOffset serverTime)
        {
            Console.WriteLine($"Server local time: {serverTime:O}");
        }

        if (_clock.ServerCorrection is TimeSpan correction)
        {
            Console.WriteLine(
                $"Saved correction: {correction.TotalSeconds:F3} seconds");

            Console.WriteLine(
                $"Corrected client time: {_clock.CorrectedTime:O}");
        }

        if(_clock.RequestTime is DateTimeOffset sent)
        {
            Console.WriteLine($"Request sent time: {sent:O}");
        }
        if(_clock.ResponseTime is DateTimeOffset received)
        {
            Console.WriteLine($"Response received time: {received:O}");
        }

        Console.WriteLine($"System RTT: {_clock.CountSystemRTT()}");
        Console.WriteLine($"Network delay: {_clock.CountNetworkDelay()}");

    }

    private static bool TryParseCommand(
        string line,
        out string method,
        out string path)
    {
        method = "";
        path = "";

        var parts = line.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        method = parts[0].ToUpperInvariant();
        path = parts[1];

        return (method is "GET" or "POST") &&
               path.StartsWith('/') &&
               !path.StartsWith("//");
    }

    private void Log(string message)
    {
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss.fff}] " +
            $"Client {_clientId} {message}");
    }
}