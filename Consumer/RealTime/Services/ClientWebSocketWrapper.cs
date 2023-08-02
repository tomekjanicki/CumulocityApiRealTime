using System.Net;
using System.Net.WebSockets;
using Consumer.Extensions;
using Consumer.Models;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public sealed class ClientWebSocketWrapper : IClientWebSocketWrapper
{
    private readonly ClientWebSocket _clientWebSocket;

    public ClientWebSocketWrapper(ICredentials credentials)
    {
        _clientWebSocket = new ClientWebSocket();
        _clientWebSocket.Options.Credentials = credentials;
    }

    public void Dispose() => 
        _clientWebSocket.Dispose();

    public ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default) => 
        _clientWebSocket.Send(utf8Bytes, cancellationToken);

    public Task<ReceiveResult> Receive(CancellationToken cancellationToken) => 
        _clientWebSocket.Receive(cancellationToken);

    public Task Close(CancellationToken cancellationToken) => 
        _clientWebSocket.Close(cancellationToken);

    public Task<Error<string>?> Connect(Uri uri, CancellationToken cancellationToken) => 
        _clientWebSocket.Connect(uri, cancellationToken);

    public WebSocketState State => _clientWebSocket.State;
}