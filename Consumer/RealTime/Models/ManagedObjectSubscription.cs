namespace Consumer.RealTime.Models;

public sealed class ManagedObjectSubscription : BaseSubscription
{
    public ManagedObjectSubscription(string name, string id)
        : base(name)
    {
        Id = id;
    }

    public string Id { get; }
}