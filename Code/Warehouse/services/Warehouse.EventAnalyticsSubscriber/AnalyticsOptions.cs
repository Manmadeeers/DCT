namespace Warehouse.EventAnalyticsSubscriber;

public sealed class AnalyticsOptions
{
    public string StateFile { get; set; } =
        "/data/statistics.json";
}