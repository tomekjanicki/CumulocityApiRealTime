namespace Consumer.RealTime.Models;

public sealed class SubscribeRequest : Request
{
    public const string Name = "/meta/subscribe";
    public string Id { get; init; } = string.Empty;

    public string Subscription { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public SubscribeRequest() 
        : base(Name)
    {
    }

    public override byte[] AsUtf8Bytes()
        => GetRequestAsBytes(this);
}