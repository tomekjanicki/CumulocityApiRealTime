using Consumer.RealTime.Models;

namespace Consumer.RealTime.Extensions;

public static class StringExtensions
{
    public static Error GetError(this string? message, bool transient) =>
        new(message is not null && transient, message ?? "Generic error.");

    public static bool IsTransient(this string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }
        var items = message.Split(':');

        return items.Length >= 3 && int.TryParse(items[0], out var value) && value is 408 or >= 500;
    }
}