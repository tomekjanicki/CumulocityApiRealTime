using System.Net.WebSockets;
using Consumer.RealTime.Extensions;
using Consumer.RealTime.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.RealTime.Services;

public sealed class RealTimeWebSocketClient2 : IRealTimeWebSocketClient2
{
    private readonly ILogger<RealTimeWebSocketClient2> _logger;
    private readonly IClientWebSocketWrapperFactory _clientWebSocketWrapperFactory;
    private readonly IDataFeedHandler _dataFeedHandler;
    private readonly Uri _uri;
    private readonly TimeSpan _operationTimeout;
    private IClientWebSocketWrapper _clientWebSocketWrapper;

    public RealTimeWebSocketClient2(ILogger<RealTimeWebSocketClient2> logger, IClientWebSocketWrapperFactory clientWebSocketWrapperFactory, IDataFeedHandler dataFeedHandler, IOptions<ConfigurationSettings> options)
    {
        _logger = logger;
        _clientWebSocketWrapperFactory = clientWebSocketWrapperFactory;
        _dataFeedHandler = dataFeedHandler;
        _uri = new Uri($"{options.Value.WebSocketUrl}notification2");
        _operationTimeout = options.Value.OperationTimeout;
        _clientWebSocketWrapper = NullClientWebSocketWrapper.Instance;
    }

    public Task<Error?> Connect(string token, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((This: this, token), _operationTimeout, static (p, cancellationToken) => p.This.ConnectInt(p.token, cancellationToken), static () => GetTimeoutError(), cancellationToken);

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        if (ClientWebSocketWrapperIsNullClientWebSocketWrapper())
        {
            return;
        }
        await DisconnectInt(_clientWebSocketWrapper, cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectInt(IClientWebSocketWrapper wrapper, CancellationToken cancellationToken)
    {
        await wrapper.Close(cancellationToken).ConfigureAwait(false);
        await wrapper.DisposeAsync().ConfigureAwait(false);
        _clientWebSocketWrapper = NullClientWebSocketWrapper.Instance;
    }

    private async Task<Error?> ConnectInt(string token, CancellationToken cancellationToken)
    {
        var uri = new Uri($"{_uri}/consumer/?token={token}");
        var clientWebSocket = _clientWebSocketWrapperFactory.GetNewInstance(_logger, uri, this, static (bytes, p, token) => p.DataHandler(bytes, token),
            static (state, p, cancellationToken) => p.MonitorHandler(state, cancellationToken));
        var connectResult = await clientWebSocket.Connect(cancellationToken).ConfigureAwait(false);
        if (connectResult is not null)
        {
            return new Error(true, connectResult.Value.Value);
        }
        _clientWebSocketWrapper = clientWebSocket;

        return null;
    }

    private Task<Error?> ReConnect(CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper(this, _operationTimeout, static (p, sources) => p.ReConnectInt(sources), static () => GetTimeoutError(), cancellationToken);

    private async Task<Error?> ReConnectInt(CancellationToken cancellationToken)
    {
        if (ClientWebSocketWrapperIsNullClientWebSocketWrapper())
        {
            throw new InvalidOperationException("Not connected.");
        }
        if (_clientWebSocketWrapper.State == WebSocketState.Open)
        {
            return null;
        }
        var connectResult = await _clientWebSocketWrapper.ReConnect(cancellationToken).ConfigureAwait(false);

        return connectResult is not null ? new Error(true, connectResult.Value.Value) : null;
    }

    private Task DataHandler(byte[] bytes, CancellationToken cancellationToken) => 
        _dataFeedHandler.Handle(bytes, cancellationToken);

    private async Task MonitorHandler(WebSocketState state, CancellationToken cancellationToken)
    {
        _logger.LogDebug("ClientWebSocketState: {State}.", state);
        if (state is WebSocketState.Aborted or WebSocketState.Closed)
        {
            await ReConnect(cancellationToken).ConfigureAwait(false);
        }
    }

    private static Error GetTimeoutError()
        => "Operation timed out.".GetError(true);

    public async ValueTask DisposeAsync()
    {
        if (!ClientWebSocketWrapperIsNullClientWebSocketWrapper())
        {
            await _clientWebSocketWrapper.DisposeAsync().ConfigureAwait(false);
        }
        _clientWebSocketWrapper = NullClientWebSocketWrapper.Instance;
    }

    private bool ClientWebSocketWrapperIsNullClientWebSocketWrapper() =>
        _clientWebSocketWrapper is NullClientWebSocketWrapper;
}