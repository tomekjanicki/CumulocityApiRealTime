using Consumer.RealTime.Models;

namespace Consumer.RealTime.Services;

public interface IRealTimeWebSocketClient : IAsyncDisposable
{
    Task<Error?> Connect(CancellationToken cancellationToken = default);
    Task<Error?> Subscribe(Subscription subscription, CancellationToken cancellationToken = default);
    Task<Error?> Unsubscribe(Subscription subscription, CancellationToken cancellationToken = default);
    Task Disconnect(CancellationToken cancellationToken = default);
    bool Connected { get; }
    IReadOnlySet<Subscription> Subscriptions { get; }
}