namespace Consumer.RealTime.Models;

public sealed class HeartbeatRequest : Request
{
    public const string Name = "/meta/connect";

    public string ClientId { get; init; } = string.Empty;

    public string ConnectionType { get; init; } = string.Empty;

    public Advice Advice { get; init; } = new();

    public HeartbeatRequest() 
        : base(Name)
    {
    }

    public override byte[] AsUtf8Bytes()
        => GetRequestAsBytes(this);
}