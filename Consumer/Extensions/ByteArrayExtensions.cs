using System.Text.Json;

namespace Consumer.Extensions;

public static class ByteArrayExtensions
{
    public static T? GetObjectFromUtf8Bytes<T>(this byte[] utf8Bytes) =>
        JsonSerializer.Deserialize<T>(utf8Bytes, Constants.CamelCaseJsonSerializerOptions);
}