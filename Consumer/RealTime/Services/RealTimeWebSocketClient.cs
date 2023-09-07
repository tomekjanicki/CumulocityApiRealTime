using System.Net.WebSockets;
using Consumer.RealTime.Extensions;
using Consumer.RealTime.Models;
using Consumer.RealTime.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = Consumer.RealTime.Models.Error;

namespace Consumer.RealTime.Services;

public sealed class RealTimeWebSocketClient : IRealTimeWebSocketClient
{
    private const string WebSocket = "websocket";
    private readonly IDataFeedHandler _dataFeedHandler;
    private readonly ILogger<RealTimeWebSocketClient> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IClientWebSocketWrapperFactory _clientWebSocketWrapperFactory;
    private readonly Ext _ext;
    private readonly Advice _defaultAdvice;
    private readonly TimeSpan _operationTimeout;
    private readonly TimeSpan _monitorDelay;
    private readonly ResponseProcessor<HandShakeResponse> _handShakeResponses;
    private readonly ResponseProcessor<SubscribeResponse> _subscribeResponses;
    private readonly ResponseProcessor<UnsubscribeResponse> _unsubscribeResponses;
    private readonly HashSet<Subscription> _subscriptions;
    private readonly HeartBeatTimes _heartBeatTimes;
    private readonly Uri _uri;
    private IRealTimeWebSocketClientArgument _argument; 

    public RealTimeWebSocketClient(IOptions<ConfigurationSettings> options, IDataFeedHandler dataFeedHandler, ILogger<RealTimeWebSocketClient> logger,
        IDateTimeProvider dateTimeProvider, IClientWebSocketWrapperFactory clientWebSocketWrapperFactory)
    {
        _dataFeedHandler = dataFeedHandler;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _clientWebSocketWrapperFactory = clientWebSocketWrapperFactory;
        _handShakeResponses = new ResponseProcessor<HandShakeResponse>();
        _subscribeResponses = new ResponseProcessor<SubscribeResponse>();
        _unsubscribeResponses = new ResponseProcessor<UnsubscribeResponse>();
        _operationTimeout = options.Value.OperationTimeout;
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
        _heartBeatTimes = new HeartBeatTimes(dateTimeProvider);
        _argument = NullRealTimeWebSocketClientArgument.Instance;
        _uri = new Uri($"{options.Value.WebSocketUrl}notification/realtime");
    }

    public Task<Error?> Connect(CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper(this, _operationTimeout, static (p, sources) => p.ConnectInt(sources), static () => GetTimeoutError(), cancellationToken);

    private  Task<Error?> ReConnect(bool full, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((This: this, full), _operationTimeout, static (p, sources) => p.This.ReConnectInt(p.full, sources), static () => GetTimeoutError(), cancellationToken);

    private async Task DataHandler(byte[] bytes, CancellationToken cancellationToken)
    {
        var channelResponse = ChannelResponse.GetResponse(bytes);
        if (channelResponse is not null)
        {
            switch (channelResponse.Channel)
            {
                case HandShakeRequest.Name:
                    _handShakeResponses.Add(HandShakeResponse.GetRequiredResponse(bytes));

                    return;
                case SubscribeRequest.Name:
                    _subscribeResponses.Add(SubscribeResponse.GetRequiredResponse(bytes));

                    return;
                case UnsubscribeRequest.Name:
                    _unsubscribeResponses.Add(UnsubscribeResponse.GetRequiredResponse(bytes));

                    return;
                case HeartbeatRequest.Name:
                    await HeartBeatHandler(HeartbeatResponse.GetRequiredResponse(bytes), cancellationToken).ConfigureAwait(false);

                    return;
            }
        }
        await _dataFeedHandler.Handle(bytes, cancellationToken).ConfigureAwait(false);
    }

    private async Task MonitorHandler(WebSocketState state, CancellationToken cancellationToken)
    {
        var timeOuted = _argument.Advice.IsTimeOuted(_heartBeatTimes, _dateTimeProvider.GetNow(), _monitorDelay);
        var fullReconnect = _argument.FullReconnect;
        _logger.LogDebug("ClientWebSocketState: {State} FullReconnect: {FullReconnect}, HeartBeats: {HeartBeats}, TimeOuted: {TimeOuted}.", state, fullReconnect, _heartBeatTimes, timeOuted);
        if (state is WebSocketState.Aborted or WebSocketState.Closed || fullReconnect || timeOuted)
        {
            await HandleAbortedOrClosedOrFullReconnectOrTimeOuted(fullReconnect, cancellationToken).ConfigureAwait(false);
        }
    }
    
    private async Task<Error?> ReConnectInt(bool full, CancellationTokenSources tokenSources)
    {
        if (ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            throw new InvalidOperationException("Not connected.");
        }
        ClearCollectionWrappers();
        _heartBeatTimes.Clear();
        if (_argument.ClientWebSocketWrapper.State != WebSocketState.Open)
        {
            var connectResult = await _argument.ClientWebSocketWrapper.ReConnect(tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
            if (connectResult is not null)
            {
                return new Error(true, connectResult.Value.Value);
            }
        }
        if (full)
        {
            _logger.LogDebug("Executing full reconnect.");
            var response = await _handShakeResponses.SendAndReceive(_argument.ClientWebSocketWrapper, tokenSources, this, static (requestId, p) => new HandShakeRequest
                {
                    Id = requestId,
                    Ext = p._ext
                },
                static response => response.ToResult(WebSocket),
                static () => GetTimeoutError()).ConfigureAwait(false);
            if (response.IsT1)
            {
                return response.AsT1;
            }
            _argument = _argument.CreateWithClientIdAndAdvice(response.AsT0, _defaultAdvice);
            await SendHeartbeat(_argument, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("Executing partial reconnect.");
            await SendHeartbeat(_argument, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        }
        
        return null;
    }

    private async Task<Error?> ConnectInt(CancellationTokenSources tokenSources)
    {
        if (!ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            return "Already connected.".GetError(false);
        }
        var clientWebSocket = _clientWebSocketWrapperFactory.GetNewInstance(_logger, _uri, this, static (bytes, p, token) => p.DataHandler(bytes, token),
            (state, p, token) => p.MonitorHandler(state, token));
        var connectResult = await clientWebSocket.Connect(tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);
        if (connectResult is not null)
        {
            return new Error(true, connectResult.Value.Value);
        }
        var response = await _handShakeResponses.SendAndReceive(clientWebSocket, tokenSources, this, static (requestId, p) => new HandShakeRequest
            {
                Id = requestId,
                Ext = p._ext
            },
            static response => response.ToResult(WebSocket),
            static () => GetTimeoutError()).ConfigureAwait(false);
        if (response.IsT1)
        {
            return response.AsT1;
        }
        _argument = new RealTimeWebSocketClientArgument(response.AsT0, clientWebSocket, _defaultAdvice, false);
        await SendHeartbeat(_argument, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);

        return null;
    }

    private async Task HeartBeatHandler(HeartbeatResponse response, CancellationToken cancellationToken)
    {
        var result = response.ToResult();
        if (result.IsT0)
        {
            _logger.LogDebug("Get successful heartbeat results.");
            _argument = _argument.CreateWithAdviceAndFullReconnect(result.AsT0, false);
            _heartBeatTimes.SetEnd();
            await SendHeartbeat(_argument, cancellationToken).ConfigureAwait(false);

            return;
        }
        _logger.LogDebug("Get error heartbeat results. {Error}", result.AsT1);
        _argument = _argument.CreateWithFullReconnect(true);
    }

    private async Task HandleAbortedOrClosedOrFullReconnectOrTimeOuted(bool fullReconnect, CancellationToken cancellationToken)
    {
        try
        {
            if (fullReconnect)
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
        _argument = _argument.CreateWithFullReconnect(false);
    }

    private async Task SendHeartbeat(IRealTimeWebSocketClientArgument argument, CancellationToken cancellationToken)
    {
        var request = new HeartbeatRequest
        {
            ClientId = argument.ClientId,
            Advice = argument.Advice,
            ConnectionType = WebSocket
        };
        await argument.ClientWebSocketWrapper.Send(request, cancellationToken).ConfigureAwait(false);
        _heartBeatTimes.SetStart();
        _logger.LogDebug("Heartbeat sent.");
    }

    private static ValueTask StopHeartbeat(IRealTimeWebSocketClientArgument argument, CancellationToken cancellationToken) => 
        argument.ClientWebSocketWrapper.Send(new StopHeartbeatRequest { ClientId = argument.ClientId }, cancellationToken);

    public Task<Error?> Subscribe(Subscription subscription, CancellationToken cancellationToken = default) =>
        Wrappers.ExecutionWrapper((subscription, This: this), _operationTimeout, static (p, sources) => p.This.SubscribeInt(p.subscription, sources), static () => GetTimeoutError(), cancellationToken);

    private async Task<Error?> SubscribeInt(Subscription subscription, CancellationTokenSources tokenSources)
    {
        if (ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            return "Connect was not called.".GetError(false);
        }
        var subscriptionString = subscription.GetSubscriptionString();
        var response = await _subscribeResponses.SendAndReceive(_argument.ClientWebSocketWrapper, tokenSources, (_argument.ClientId, subscriptionString), static (requestId, p) => new SubscribeRequest
        {
                Id = requestId,
                ClientId = p.ClientId,
                Subscription = p.subscriptionString,
        },
            static response => response.ToResult(),
            static () => GetTimeoutError()).ConfigureAwait(false);
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
        if (ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            return "Connect was not called.".GetError(false);
        }
        var subscriptionString = subscription.GetSubscriptionString();
        var response = await _unsubscribeResponses.SendAndReceive(_argument.ClientWebSocketWrapper, tokenSources, (_argument.ClientId, subscriptionString), static (requestId, p) => new UnsubscribeRequest
        {
                Id = requestId,
                ClientId = p.ClientId,
                Subscription = p.subscriptionString,
            },
            static response => response.ToResult(),
            static () => GetTimeoutError()).ConfigureAwait(false);
        if (response is null)
        {
            _subscriptions.Remove(subscription);
        }

        return response;
    }

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        if (ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            return;
        }
        await StopHeartbeat(_argument, cancellationToken).ConfigureAwait(false);
        await DisconnectInt(_argument, cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectInt(IRealTimeWebSocketClientArgument argument, CancellationToken cancellationToken)
    {
        await argument.ClientWebSocketWrapper.Close(cancellationToken).ConfigureAwait(false);
        _subscriptions.Clear();
        _heartBeatTimes.Clear();
        ClearCollectionWrappers();
        await argument.ClientWebSocketWrapper.DisposeAsync().ConfigureAwait(false);
        _argument = NullRealTimeWebSocketClientArgument.Instance;
    }

    public bool Connected => !ArgumentIsNullRealTimeWebSocketClientArgument();
    public IReadOnlySet<Subscription> Subscriptions => _subscriptions;

    private static Error GetTimeoutError()
        => "Operation timed out.".GetError(true);

    public async ValueTask DisposeAsync()
    {
        _subscriptions.Clear();
        _heartBeatTimes.Clear();
        ClearCollectionWrappers();
        if (!ArgumentIsNullRealTimeWebSocketClientArgument())
        {
            await _argument.ClientWebSocketWrapper.DisposeAsync().ConfigureAwait(false);
        }
        _argument = NullRealTimeWebSocketClientArgument.Instance;
    }

    private void ClearCollectionWrappers()
    {
        _handShakeResponses.Clear();
        _subscribeResponses.Clear();
        _unsubscribeResponses.Clear();
    }

    private bool ArgumentIsNullRealTimeWebSocketClientArgument() => 
        _argument is NullRealTimeWebSocketClientArgument;
}