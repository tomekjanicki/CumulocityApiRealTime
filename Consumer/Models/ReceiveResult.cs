namespace Consumer.Models;

public sealed class ReceiveResult
{
    public ReceiveResult(bool close, byte[] data)
    {
        Close = close;
        Data = data;
    }

    public bool Close { get; }

    public byte[] Data { get; }
}