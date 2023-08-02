using System.Net.WebSockets;
using Consumer.Models;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public interface IClientWebSocketWrapper : IDisposable
{
    ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default);

    Task<ReceiveResult> Receive(CancellationToken cancellationToken = default);

    Task Close(CancellationToken cancellationToken = default);

    Task<Error<string>?> Connect(Uri uri, CancellationToken cancellationToken = default);

    WebSocketState State { get; }
}