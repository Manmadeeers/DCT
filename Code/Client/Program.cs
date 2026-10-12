using System.Text;

if (args.Length < 2 || !int.TryParse(args[0], out var id) || id <= 0 ||
!Uri.TryCreate(args[1], UriKind.Absolute, out var server) ||
    (server.Scheme != "http" && server.Scheme != "https"))
{
    Console.WriteLine("Usage: dotnet run -- <client-id> <server-url>");
    return;
}

using var client = new HttpClient { BaseAddress = server, Timeout = TimeSpan.FromSeconds(60) };
client.DefaultRequestHeaders.Add("X-Client-Id", id.ToString());
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
Console.WriteLine($"Client {id}. Enter: METHOD /api/path. Empty line exits. Ctrl+C cancels.");

while (!stop.IsCancellationRequested)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) break;
    var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2 || (parts[0].ToUpperInvariant() is not ("GET" or "POST")) ||
        !parts[1].StartsWith('/') || parts[1].StartsWith("//"))
    {
        Console.WriteLine("Enter GET or POST and a path starting with /.");
        continue;
    }
    using var request = new HttpRequestMessage(new HttpMethod(parts[0].ToUpperInvariant()), parts[1]);
    if (request.Method == HttpMethod.Post)
    {
        Console.Write("JSON: ");
        request.Content = new StringContent(Console.ReadLine() ?? "", Encoding.UTF8,
            "application/json");
    }
    try
    {
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] Client {id} -> SEND");
        using var response = await client.SendAsync(request, stop.Token);
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] Client {id} <- HTTP {(int)response.StatusCode}");
        Console.WriteLine(await response.Content.ReadAsStringAsync(stop.Token));
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Server unavailable or connection lost: {ex.Message}");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine(stop.IsCancellationRequested ? "Request cancelled." : "Server response timed out.");
    }
}