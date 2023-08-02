using System.Net.WebSockets;
using Consumer.Models;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public interface IClientWebSocketWrapper : IDisposable
{
    ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default);

    Task<ReceiveResult> Receive(CancellationToken cancellationToken);

    Task Close(CancellationToken cancellationToken);

    Task<Error<string>?> Connect(Uri uri, CancellationToken cancellationToken);

    WebSocketState State { get; }
}