using Consumer.RealTime.Models.Dtos;
using Consumer.RealTime.Services;

namespace Consumer.RealTime.Models;

public interface IRealTimeWebSocketClientArgument
{
    string ClientId { get; }

    IClientWebSocketWrapper ClientWebSocketWrapper { get; }

    Advice Advice { get; }

    bool FullReconnect { get; }

    IRealTimeWebSocketClientArgument CreateWithAdviceAndFullReconnect(Advice advice, bool fullReconnect);

    IRealTimeWebSocketClientArgument CreateWithClientIdAndAdvice(string clientId, Advice advice);

    IRealTimeWebSocketClientArgument CreateWithFullReconnect(bool fullReconnect);
}