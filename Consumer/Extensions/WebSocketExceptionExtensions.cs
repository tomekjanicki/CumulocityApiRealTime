using System.Net.WebSockets;

namespace Consumer.Extensions;

public static class WebSocketExceptionExtensions
{
    public static bool IsConnectionError(this WebSocketException exception) => 
        exception.WebSocketErrorCode == WebSocketError.Faulted;
}