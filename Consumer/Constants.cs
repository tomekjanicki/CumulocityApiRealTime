using System.Text.Json;

namespace Consumer;

public static class Constants
{
    static Constants() =>
        CamelCaseJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public static JsonSerializerOptions CamelCaseJsonSerializerOptions { get; }
}