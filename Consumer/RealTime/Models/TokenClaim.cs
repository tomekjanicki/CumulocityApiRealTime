namespace Consumer.RealTime.Models;

public sealed class TokenClaim
{
    public TokenClaim(string subscriber, string subscription)
    {
        Subscriber = subscriber;
        Subscription = subscription;
    }

    public string Subscriber { get; }

    public string Subscription { get; }

    public int? ExpiresInMinutes { get; set; }

    public bool? Signed { get; set; }

    public bool? NonPersistent { get; set; }

    public bool? Shared { get; set; }
}