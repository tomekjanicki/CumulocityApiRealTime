using System.Net;
using System.Net.WebSockets;
using Consumer.RealTime.Extensions;
using Consumer.RealTime.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.RealTime.Services;

public sealed class RealTimeWebSocketClient : IRealTimeWebSocketClient
{
    private const string WebSocket = "websocket";
    private readonly IDataFeedHandler _dataFeedHandler;
    private readonly ILogger<RealTimeWebSocketClient> _logger;
    private readonly IClientWebSocketWrapperFactory _clientWebSocketWrapperFactory;
    private readonly Ext _ext;
    private readonly Uri _uri;
    private readonly ConcurrentCollectionWrapper<HandShakeResponse> _handShakeResponses;
    private readonly ConcurrentCollectionWrapper<SubscribeResponse> _subscribeResponses;
    private readonly ConcurrentCollectionWrapper<UnsubscribeResponse> _unsubscribeResponses;
    private readonly Advice _defaultAdvice;
    private readonly TimeSpan _operationTimeout;
    private readonly TimeSpan _minimalDelay;
    private readonly TimeSpan _monitorDelay;
    private readonly ICredentials _credentials;
    private readonly HashSet<Subscription> _subscriptions;
    private readonly HeartBeatTimes _heartBeatTimes;
    private IClientWebSocketWrapper? _clientWebSocket;
    private TaskData<ReceiveHandler.Dependencies<RealTimeWebSocketClient>>? _receiveHandlerTaskData;
    private TaskData<MonitorHandler.Dependencies<RealTimeWebSocketClient>>? _monitorHandlerTaskData;
    private string? _clientId;
    private Advice? _serverAdvice;
    private bool _fullReconnect;
    
    public RealTimeWebSocketClient(IOptions<ConfigurationSettings> options, IDataFeedHandler dataFeedHandler, ILogger<RealTimeWebSocketClient> logger,
        IClientWebSocketWrapperFactory clientWebSocketWrapperFactory)
    {
        _dataFeedHandler = dataFeedHandler;
        _logger = logger;
        _clientWebSocketWrapperFactory = clientWebSocketWrapperFactory;
        _uri = options.Value.ApiUri;
        _credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
        _handShakeResponses = new ConcurrentCollectionWrapper<HandShakeResponse>();
        _subscribeResponses = new ConcurrentCollectionWrapper<SubscribeResponse>();
        _unsubscribeResponses = new ConcurrentCollectionWrapper<UnsubscribeResponse>();
        _operationTimeout = options.Value.OperationTimeout;
        _minimalDelay = TimeSpan.FromMilliseconds(1);
        _monitorDelay = options.Value.WebSocketClientMonitorInterval;
        _ext = new Ext
        {
            Auth = Auth.GetWithUserNameAndPassword(options.Value.UserName, options.Value.Password)
        };
        _defaultAdvice = new Advice
        {
            Interval = options.Value.HeartBeatIntervalInMilliseconds,
            Timeout = options.Value.HeartBeatTimeoutInMilliseconds
        };
        _subscriptions = new HashSet<Subscription>();
        _heartBeatTimes = new HeartBeatTimes();
    }

    public Task<Error?> Connect(CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper(this, _operationTimeout, static (p, sources) => p.ConnectInt(sources), static () => GetTimeoutError(), cancellationToken);

    private Task<Error?> ReConnect(bool full, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((This: this, full), _operationTimeout, static (p, sources) => p.This.ReConnectInt(p.full, sources), static () => GetTimeoutError(), cancellationToken);

    private async Task<Error?> ReConnectInt(bool full, CancellationTokenSources tokenSources)
    {
        if (_clientId is null)
        {
            throw new InvalidOperationException("Not connected.");
        }
        ClearCollectionWrappers();
        _heartBeatTimes.Clear();
        if (_clientWebSocket is null || _clientWebSocket.State != WebSocketState.Open)
        {
            _clientWebSocket?.Dispose();
            _clientWebSocket = _clientWebSocketWrapperFactory.GetNewInstance(_credentials);
            var connectResult = await _clientWebSocket.Connect(_uri, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
            if (connectResult is not null)
            {
                return new Error(true, connectResult.Value.Value);
            }
        }
        if (full)
        {
            _logger.LogDebug("Executing full reconnect.");
            var requestId = Guid.NewGuid().ToString();
            await SendHandShakeRequest(requestId, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
            var handShakeResponse = Wrappers.HandleResponse((_handShakeResponses, requestId),
                static p => p._handShakeResponses.TryGetAndRemove(p.requestId),
                static response => response.ToResult(WebSocket),
                static () => GetTimeoutError(),
                tokenSources);
            if (handShakeResponse.IsT1)
            {
                return handShakeResponse.AsT1;
            }
            _clientId = handShakeResponse.AsT0;
            await SendHeartbeat(_clientId, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("Executing partial reconnect.");
            await SendHeartbeat(_clientId, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        }
        
        return null;
    }

    private ValueTask SendHandShakeRequest(string id, CancellationToken cancellationToken) => 
        _clientWebSocket!.Send(new HandShakeRequest { Ext = _ext, Id = id }, cancellationToken);

    private async Task<Error?> ConnectInt(CancellationTokenSources tokenSources)
    {
        if (_clientId is not null)
        {
            return "Already connected.".GetError(false);
        }
        _clientWebSocket = _clientWebSocketWrapperFactory.GetNewInstance(_credentials);
        var connectResult = await _clientWebSocket.Connect(_uri, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        if (connectResult is not null)
        {
            return new Error(true, connectResult.Value.Value);
        }
        _receiveHandlerTaskData = new TaskData<ReceiveHandler.Dependencies<RealTimeWebSocketClient>>(GetReceiveHandlerDependencies(), static (source, p) => ReceiveHandler.Handler(p, source.Token));
        _monitorHandlerTaskData = new TaskData<MonitorHandler.Dependencies<RealTimeWebSocketClient>>(GetMonitorHandlerDependencies(), static (source, p) => MonitorHandler.Handler(p, source.Token));
        var requestId = Guid.NewGuid().ToString();
        await SendHandShakeRequest(requestId, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        var handShakeResponse = Wrappers.HandleResponse((_handShakeResponses, requestId),
            static p => p._handShakeResponses.TryGetAndRemove(p.requestId),
            static response => response.ToResult(WebSocket),
            static () => GetTimeoutError(),
            tokenSources);
        if (handShakeResponse.IsT1)
        {
            return handShakeResponse.AsT1;
        }
        _clientId = handShakeResponse.AsT0;
        await SendHeartbeat(_clientId, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);

        return null;
    }

    private ReceiveHandler.Dependencies<RealTimeWebSocketClient> GetReceiveHandlerDependencies() =>
        new(_logger, _clientWebSocket, _handShakeResponses,
            _subscribeResponses, _unsubscribeResponses, _minimalDelay, _dataFeedHandler,
            static (response, p, token) => p.HeartBeatHandler(response, token), this);

    private MonitorHandler.Dependencies<RealTimeWebSocketClient> GetMonitorHandlerDependencies() =>
        new(_logger, _clientWebSocket, _minimalDelay, _monitorDelay, static p => p.GetAdvice(),
            static p => p._fullReconnect, _heartBeatTimes, static () => DateTime.Now, static (p, token) => p.HandleAbortedOrClosedOrFullReconnectOrTimeOuted(token), this);

    private async Task HeartBeatHandler(HeartbeatResponse response, CancellationToken cancellationToken)
    {
        var result = response.ToResult();
        if (result.IsT0)
        {
            _logger.LogDebug("Get successful heartbeat results.");
            _serverAdvice = result.AsT0;
            _heartBeatTimes.SetEnd();
            await SendHeartbeat(_clientId!, cancellationToken).ConfigureAwait(false);
            _fullReconnect = false;

            return;
        }
        _logger.LogDebug("Get error heartbeat results. {Error}", result.AsT1);
        _fullReconnect = true;
    }

    private async Task HandleAbortedOrClosedOrFullReconnectOrTimeOuted(CancellationToken cancellationToken)
    {
        try
        {
            if (_fullReconnect)
            {
                await ExecuteFullReConnect(cancellationToken).ConfigureAwait(false);

                return;
            }
            await ExecutePartialReConnect(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Error during reconnect.");
        }
    }

    private async Task ExecutePartialReConnect(CancellationToken cancellationToken)
    {
        var reConnect = await ReConnect(false, cancellationToken).ConfigureAwait(false);
        if (reConnect is not null)
        {
            _logger.LogDebug("Error during reconnect. {Error}", reConnect);

            return;
        }

        _logger.LogDebug("Reconnected.");
    }

    private async Task ExecuteFullReConnect(CancellationToken cancellationToken)
    {
        var fullReConnect = await ReConnect(true, cancellationToken).ConfigureAwait(false);
        if (fullReConnect is not null)
        {
            _logger.LogDebug("Error during reconnect. {Error}", fullReConnect);

            return;
        }
        _logger.LogDebug("Reconnected.");
        foreach (var subscription in _subscriptions)
        {
            var subscribe = await Subscribe(subscription, cancellationToken).ConfigureAwait(false);
            if (subscribe is not null)
            {
                _logger.LogDebug("Error during subscribe. {Error}", subscribe);
            }
            else
            {
                _logger.LogDebug("Subscribed.");
            }
        }
        _fullReconnect = false;
    }

    private async Task SendHeartbeat(string clientId, CancellationToken cancellationToken)
    {
        var request = new HeartbeatRequest
        {
            ClientId = clientId,
            Advice = GetAdvice(),
            ConnectionType = WebSocket
        };
        await _clientWebSocket!.Send(request, cancellationToken).ConfigureAwait(false);
        _heartBeatTimes.SetStart();
        _logger.LogDebug("Heartbeat sent.");
    }

    private Advice GetAdvice() => _serverAdvice ?? _defaultAdvice;

    private ValueTask StopHeartbeat(string clientId, CancellationToken cancellationToken) => 
        _clientWebSocket!.Send(new StopHeartbeatRequest { ClientId = clientId }, cancellationToken);

    public Task<Error?> Subscribe(Subscription subscription, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((subscription, This: this), _operationTimeout, static (p, sources) => p.This.SubscribeInt(p.subscription, sources), static () => GetTimeoutError(), cancellationToken);

    private async Task<Error?> SubscribeInt(Subscription subscription, CancellationTokenSources tokenSources)
    {
        if (_clientId is null)
        {
            return "Connect was not called.".GetError(false);
        }
        var subscriptionString = subscription.GetSubscriptionString();
        var requestId = Guid.NewGuid().ToString();
        var request = new SubscribeRequest
        {
            ClientId = _clientId,
            Subscription = subscriptionString,
            Id = requestId
        };
        await _clientWebSocket!.Send(request, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        var response = Wrappers.HandleResponse((_subscribeResponses, requestId),
            static p => p._subscribeResponses.TryGetAndRemove(p.requestId),
            static response => response.ToResult(),
            static () => GetTimeoutError(),
            tokenSources);
        if (response is null)
        {
            _subscriptions.Add(subscription);
        }

        return response;
    }

    public Task<Error?> Unsubscribe(Subscription subscription, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((subscription, This: this), _operationTimeout, static (p, sources) => p.This.UnsubscribeInt(p.subscription, sources), static () => GetTimeoutError(), cancellationToken);

    private async Task<Error?> UnsubscribeInt(Subscription subscription, CancellationTokenSources tokenSources)
    {
        if (_clientId is null)
        {
            return "Connect was not called.".GetError(false);
        }
        var subscriptionString = subscription.GetSubscriptionString();
        var requestId = Guid.NewGuid().ToString();
        var request = new UnsubscribeRequest
        {
            ClientId = _clientId,
            Subscription = subscriptionString,
            Id = requestId
        };
        await _clientWebSocket!.Send(request, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        var response = Wrappers.HandleResponse((_unsubscribeResponses, requestId),
            static p => p._unsubscribeResponses.TryGetAndRemove(p.requestId),
            static response => response.ToResult(),
            static () => GetTimeoutError(),
            tokenSources);
        if (response is null)
        {
            _subscriptions.Remove(subscription);
        }

        return response;
    }

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        if (_clientId is null)
        {
            return;
        }
        await StopHeartbeat(_clientId!, cancellationToken).ConfigureAwait(false);
        await DisconnectInt(cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectInt(CancellationToken cancellationToken)
    {
        await _clientWebSocket!.Close(cancellationToken).ConfigureAwait(false);
        await _receiveHandlerTaskData!.Stop().ConfigureAwait(false);
        await _monitorHandlerTaskData!.Stop().ConfigureAwait(false);
        _subscriptions.Clear();
        _heartBeatTimes.Clear();
        ClearCollectionWrappers();
        _clientWebSocket!.Dispose();
        _fullReconnect = false;
        SetToNull();
    }

    public bool Connected => _clientId is not null;
    public IReadOnlySet<Subscription> Subscriptions => _subscriptions;

    private static Error GetTimeoutError()
        => "Operation timed out.".GetError(true);

    public void Dispose()
    {
        _subscriptions.Clear();
        _heartBeatTimes.Clear();
        ClearCollectionWrappers();
        _clientWebSocket?.Dispose();
        _monitorHandlerTaskData?.Dispose();
        _receiveHandlerTaskData?.Dispose();
        _fullReconnect = false;
        SetToNull();
    }

    private void SetToNull()
    {
        _clientWebSocket = null;
        _monitorHandlerTaskData = null;
        _receiveHandlerTaskData = null;
        _clientId = null;
        _serverAdvice = null;
    }

    private void ClearCollectionWrappers()
    {
        _handShakeResponses.Clear();
        _subscribeResponses.Clear();
        _unsubscribeResponses.Clear();
    }
}