using System.Runtime.InteropServices;

namespace Client.Clock;


public sealed class ClientClock
{
    private readonly TimeSpan _configuredOffset;
    public DateTimeOffset? ServerTime { get; private set; }
    public DateTimeOffset? RequestTime { get; private set; }
    public DateTimeOffset? ResponseTime { get; private set; }

    public ClientClock(TimeSpan configuredOffset)
    {
        _configuredOffset = configuredOffset;
    }

    public TimeSpan? ServerCorrection { get; private set; }

    public DateTimeOffset LocalTime =>
        DateTimeOffset.Now + _configuredOffset;

    public DateTimeOffset CorrectedTime =>
        LocalTime + (ServerCorrection ?? TimeSpan.Zero);

    public void UpdateCorrection(
        DateTimeOffset serverTime,
        DateTimeOffset? clientTimeAtReceipt)
    {
        ServerTime = serverTime;
        ServerCorrection = serverTime - clientTimeAtReceipt;
    }

    public void RecordRequestSent()
    {
        RequestTime = LocalTime;
        ResponseTime = null;
    }

    public string CountSystemRTT()
    {
        return (ResponseTime-RequestTime).ToString();
    }

    public string CountNetworkDelay()
    {
        return ((ResponseTime-RequestTime)/2).ToString();
    }

    public DateTimeOffset RecordResponseReceived()
    {
        var time = LocalTime;
        ResponseTime = time;
        return time;
    }
}