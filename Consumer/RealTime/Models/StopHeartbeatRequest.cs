namespace Consumer.RealTime.Models;

public sealed class StopHeartbeatRequest : Request
{
    public const string Name = "/meta/disconnect";

    public string ClientId { get; init; } = string.Empty;

    public StopHeartbeatRequest() 
        : base(Name)
    {
    }

    public override byte[] AsUtf8Bytes()
        => GetRequestAsBytes(this);
}