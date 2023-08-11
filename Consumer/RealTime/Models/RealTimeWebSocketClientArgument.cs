using Consumer.RealTime.Models.Dtos;
using Consumer.RealTime.Services;

namespace Consumer.RealTime.Models;

public sealed class RealTimeWebSocketClientArgument : IRealTimeWebSocketClientArgument
{
    public RealTimeWebSocketClientArgument(string clientId, IClientWebSocketWrapper clientWebSocketWrapper, Advice advice, bool fullReconnect)
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
        new RealTimeWebSocketClientArgument(ClientId, ClientWebSocketWrapper, advice, fullReconnect);

    public IRealTimeWebSocketClientArgument CreateWithClientIdAndAdvice(string clientId, Advice advice) =>
        new RealTimeWebSocketClientArgument(clientId, ClientWebSocketWrapper, advice, FullReconnect);

    public IRealTimeWebSocketClientArgument CreateWithFullReconnect(bool fullReconnect) =>
        new RealTimeWebSocketClientArgument(ClientId, ClientWebSocketWrapper, Advice, fullReconnect);
}