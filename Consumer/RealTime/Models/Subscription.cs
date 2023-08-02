namespace Consumer.RealTime.Models;

public readonly record struct Subscription(string? EntityId, NotificationType Type)
{
    private const string Alarms = "alarms";
    private const string ManagedObjects = "managedobjects";
    private const string Measurements = "measurements";
    private const string Events = "events";

    public string GetSubscriptionString() =>
        Type switch
        {
            NotificationType.Alarm => $"/{Alarms}/{GetEntityId(EntityId)}",
            NotificationType.Measurement => $"/{Measurements}/{GetEntityId(EntityId)}",
            NotificationType.Events => $"/{Events}/{GetEntityId(EntityId)}",
            _ => $"/{ManagedObjects}/{GetEntityId(EntityId)}"
        };
    private static string GetEntityId(string? entityId)
        => entityId ?? "*";
}