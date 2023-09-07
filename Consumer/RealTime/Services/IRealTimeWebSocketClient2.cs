using Consumer.RealTime.Models;

namespace Consumer.RealTime.Services;

public interface IRealTimeWebSocketClient2 : IAsyncDisposable
{
    Task<Error?> Connect(string token, CancellationToken cancellationToken = default);

    Task Disconnect(CancellationToken cancellationToken = default);
}