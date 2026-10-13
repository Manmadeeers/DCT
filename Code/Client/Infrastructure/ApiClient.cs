using System.Globalization;
using System.Text;
using Client.Clock;

namespace Client.API;

public sealed class WarehouseApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ClientClock _clock;

    public WarehouseApiClient(
        int clientId,
        Uri server,
        ClientClock clock)
    {
        _clock = clock;

        _httpClient = new HttpClient
        {
            BaseAddress = server,
            Timeout = TimeSpan.FromSeconds(60)
        };

        _httpClient.DefaultRequestHeaders.Add(
            "X-Client-Id",
            clientId.ToString());
    }

    public async Task<(int StatusCode, string Body)> SendAsync(
        string method,
        string path,
        string? json,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            path);

        if (request.Method == HttpMethod.Post)
        {
            request.Content = new StringContent(
                json ?? "",
                Encoding.UTF8,
                "application/json");
        }


        _clock.RecordRequestSent();
        using var response = await _httpClient.SendAsync(request, ct);

        var clientTimeAtReceipt = _clock.RecordResponseReceived();

        if (response.Headers.TryGetValues(
            "X-Server-Time",
            out var values))
        {
            foreach (var value in values)
            {
                if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var serverTime))
                {
                    _clock.UpdateCorrection(
                        serverTime,
                        clientTimeAtReceipt);

                    break;
                }
            }
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        return ((int)response.StatusCode, body);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}