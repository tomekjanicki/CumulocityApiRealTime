using Consumer.RealTime.Models.Dtos;
using Consumer.RealTime.Services;

namespace Consumer.RealTime.Models;

public sealed class NullRealTimeWebSocketClientArgument : IRealTimeWebSocketClientArgument
{
    private NullRealTimeWebSocketClientArgument(string clientId, IClientWebSocketWrapper clientWebSocketWrapper, Advice advice, bool fullReconnect)
    {
        ClientId = clientId;
        ClientWebSocketWrapper = clientWebSocketWrapper;
        Advice = advice;
        FullReconnect = fullReconnect;
    }

    public string ClientId { get; }
    public IClientWebSocketWrapper ClientWebSocketWrapper { get; }
    public Advice Advice { get; }
    public bool FullReconnect { get; }
    public IRealTimeWebSocketClientArgument CreateWithAdviceAndFullReconnect(Advice advice, bool fullReconnect) =>
        Instance;

    public IRealTimeWebSocketClientArgument CreateWithClientIdAndAdvice(string clientId, Advice advice) =>
        Instance;

    public IRealTimeWebSocketClientArgument CreateWithFullReconnect(bool fullReconnect) =>
        Instance;

    public static readonly NullRealTimeWebSocketClientArgument Instance = new(string.Empty, new NullClientWebSocketWrapper(), new Advice(), false);
}