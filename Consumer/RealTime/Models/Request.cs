using System.Text.Json;

namespace Consumer.RealTime.Models;

public abstract class Request
{
    protected Request(string channel)
    {
        Channel = channel;
    }

    public string Channel { get; }

    public abstract byte[] AsUtf8Bytes();

    protected static byte[] GetRequestAsBytes<T>(T request) =>
        GetObjectAsUtf8Bytes(new List<T> { request });

    private static byte[] GetObjectAsUtf8Bytes<T>(T obj) => 
        JsonSerializer.SerializeToUtf8Bytes(obj, Constants.CamelCaseJsonSerializerOptions);
}