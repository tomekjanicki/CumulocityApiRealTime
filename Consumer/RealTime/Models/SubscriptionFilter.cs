namespace Consumer.RealTime.Models;

public sealed class SubscriptionFilter
{
    public string? Type { get; set; }

    public IReadOnlyCollection<string> Apis { get; set; } = Array.Empty<string>();
}