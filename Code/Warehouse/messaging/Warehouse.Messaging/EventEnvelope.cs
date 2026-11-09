using System.Text.Json;

namespace Warehouse.Messaging;

public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAtUtc,
    JsonElement Data);

public static class EventJson
{
    public static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web);
}