using Consumer.Extensions;

namespace Consumer.RealTime.Models;

public sealed class ChannelResponse
{
    public string? Channel { get; init; }

    public static ChannelResponse? GetResponse(byte[] message) => 
        message.GetObjectFromUtf8Bytes<IReadOnlyCollection<ChannelResponse>>()?.FirstOrDefault();
}