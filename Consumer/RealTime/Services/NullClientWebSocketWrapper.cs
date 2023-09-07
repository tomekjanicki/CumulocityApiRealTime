using System.Net.WebSockets;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public sealed class NullClientWebSocketWrapper : IClientWebSocketWrapper
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task<Error<string>?> ReConnect(CancellationToken cancellationToken = default) => Task.FromResult<Error<string>?>(null);

    public Task<Error<string>?> Connect(CancellationToken cancellationToken = default) => Task.FromResult<Error<string>?>(null);

    public ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public Task Close(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public WebSocketState? State => null;

    private NullClientWebSocketWrapper()
    {
    }

    public static readonly NullClientWebSocketWrapper Instance = new();
}