namespace Consumer.RealTime.Models;

public sealed class HandShakeRequest : Request
{
    public const string Name = "/meta/handshake";

    public string Id { get; init; } = string.Empty;
    public string Version => "1.0";

    public Ext Ext { get; init; } = new();

    public HandShakeRequest() 
        : base(Name)
    {
    }

    public override byte[] AsUtf8Bytes()
        => GetRequestAsBytes(this);
}