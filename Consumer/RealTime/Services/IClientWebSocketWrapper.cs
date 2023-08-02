using System.Net.WebSockets;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public interface IClientWebSocketWrapper : IAsyncDisposable
{
    Task<Error<string>?> ReConnect(CancellationToken cancellationToken = default);
    Task<Error<string>?> Connect(CancellationToken cancellationToken = default);
    ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default);
    Task Close(CancellationToken cancellationToken = default);
    WebSocketState? State { get; }
}