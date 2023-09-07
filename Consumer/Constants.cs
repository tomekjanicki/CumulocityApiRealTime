using System.Text.Json;
using Consumer.RealTime.Models;

namespace Consumer;

public static class Constants
{
    static Constants() =>
        CamelCaseJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public static JsonSerializerOptions CamelCaseJsonSerializerOptions { get; }

    public const string ClientName = "notification";

    public static readonly ApiError NullResultApiError = new("Result is null.", null);
}