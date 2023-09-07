namespace Consumer.RealTime.Models;

public abstract class BaseSubscription
{
    protected BaseSubscription(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyCollection<string> FragmentsToCopy { get; set; } = Array.Empty<string>();

    public SubscriptionFilter? Filter { get; set; }
}