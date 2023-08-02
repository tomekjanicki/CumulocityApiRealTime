namespace Consumer.RealTime.Models.Dtos;

public sealed class UnsubscribeRequest : Request
{
    public const string Name = "/meta/unsubscribe";

    public string Id { get; init; } = string.Empty;

    public string Subscription { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public UnsubscribeRequest() :
        base(Name)
    {
    }

    public override byte[] AsUtf8Bytes()
        => GetRequestAsBytes(this);
}