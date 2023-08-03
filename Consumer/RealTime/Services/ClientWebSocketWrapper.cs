using System.Net.WebSockets;
using Consumer.Extensions;using Consumer.Models;
using Consumer.RealTime.Models;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Consumer.RealTime.Services;

public sealed class ClientWebSocketWrapper<TParam> : IClientWebSocketWrapper
{
    private readonly Uri _uri;
    private readonly ILogger _logger;
    private readonly TimeSpan _monitorDelay;
    private readonly Func<byte[], TParam, CancellationToken, Task> _dataHandler;
    private readonly Func<WebSocketState, TParam, CancellationToken, Task> _monitorHandler;
    private readonly TParam _param;
    private readonly TimeSpan _minimalDelay;
    private Argument<ClientWebSocketWrapper<TParam>>? _argument;

    public ClientWebSocketWrapper(Uri uri, ILogger logger, TimeSpan monitorDelay, Func<byte[], TParam, CancellationToken, Task> dataHandler,
        Func<WebSocketState, TParam, CancellationToken, Task> monitorHandler, TParam param)
    {
        _uri = uri;
        _logger = logger;
        _monitorDelay = monitorDelay;
        _dataHandler = dataHandler;
        _monitorHandler = monitorHandler;
        _param = param;
        _minimalDelay = TimeSpan.FromMilliseconds(1);
    }

    public async Task<Error<string>?> ReConnect(CancellationToken cancellationToken = default)
    {
        if (_argument is null)
        {
            return await Connect(cancellationToken).ConfigureAwait(false);
        }
        _argument.RecreateClient();

        return await _argument.Connect(_uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Error<string>?> Connect(CancellationToken cancellationToken = default)
    {
        if (_argument is not null)
        {
            return null;
        }
        _argument = new Argument<ClientWebSocketWrapper<TParam>>(new TaskData<ClientWebSocketWrapper<TParam>>(this, static (wrapper, source) => wrapper.ReceiveHandler(source.Token)),
            new TaskData<ClientWebSocketWrapper<TParam>>(this, static (wrapper, source) => wrapper.MonitorHandler(source.Token)));

        return await _argument.Connect(_uri, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default) => 
        _argument?.Send(utf8Bytes, cancellationToken) ?? throw new InvalidOperationException("Client is null.");

    public async Task Close(CancellationToken cancellationToken = default)
    {
        if (_argument is null)
        {
            return;
        }
        await _argument.Close(cancellationToken).ConfigureAwait(false);
        _argument = null;
    }

    public WebSocketState? State => _argument?.State;

    public async ValueTask DisposeAsync()
    {
        if (_argument is not null)
        {
            await _argument.Close(CancellationToken.None).ConfigureAwait(false);
        }
        _argument = null;
    }

    private async Task HandlerWrapper<T>(T param, Action<Exception, T> outerLoggerAction, Action<Exception, T> innerLoggerAction,
        Func<T, Argument<ClientWebSocketWrapper<TParam>>, CancellationToken, Task> jobTask, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var argument = _argument;
                    if (argument is null)
                    {
                        await Task.Delay(_minimalDelay, cancellationToken).ConfigureAwait(false);

                        continue;
                    }
                    await jobTask(param, argument, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    innerLoggerAction(e, param);
                }
            }
        }
        catch (Exception e)
        {
            outerLoggerAction(e, param);
        }
    }

    private Task MonitorHandler(CancellationToken cancellationToken) =>
        HandlerWrapper(this,
            static (exception, p) => p._logger.LogDebug(exception, "Generic error in external MonitorHandler."),
            static (exception, p) => p._logger.LogDebug(exception, "Generic error in MonitorHandler."),
            static (p, argument, token) => p.InternalMonitorHandler(argument, token), cancellationToken);

    private async Task InternalMonitorHandler(Argument<ClientWebSocketWrapper<TParam>> argument, CancellationToken token)
    {
        await _monitorHandler(argument.State, _param, token).ConfigureAwait(false);
        await Task.Delay(_monitorDelay, token).ConfigureAwait(false);
    }

    private Task ReceiveHandler(CancellationToken cancellationToken) =>
        HandlerWrapper(this,
            static (exception, p) => p._logger.LogDebug(exception, "Generic error in external ReceiveHandler."),
            static (exception, p) => p._logger.LogDebug(exception, "Generic error in ReceiveHandler."),
            static (p, argument, token) => p.InternalReceiveHandler(argument, token), cancellationToken);

    private async Task InternalReceiveHandler(Argument<ClientWebSocketWrapper<TParam>> argument, CancellationToken token)
    {
        if (argument.State != WebSocketState.Open)
        {
            await Task.Delay(_minimalDelay, token).ConfigureAwait(false);

            return;
        }
        var result = await argument.Receive(token).ConfigureAwait(false);
        if (result.Close)
        {
            return;
        }
        await _dataHandler(result.Data, _param, token).ConfigureAwait(false);
    }

    private sealed class Argument<T>
    {
        private readonly TaskData<T> _receiveTaskData;
        private readonly TaskData<T> _monitorTaskData;
        private ClientWebSocket _clientWebSocket;

        public Argument(TaskData<T> receiveTaskData, TaskData<T> monitorTaskData)
        {
            _clientWebSocket = new ClientWebSocket();
            _receiveTaskData = receiveTaskData;
            _monitorTaskData = monitorTaskData;
        }

        public void RecreateClient()
        {
            _clientWebSocket.Dispose();
            _clientWebSocket = new ClientWebSocket();
        }

        public ValueTask Send(byte[] utf8Bytes, CancellationToken cancellationToken = default) =>
            _clientWebSocket.Send(utf8Bytes, cancellationToken);

        public Task<Error<string>?> Connect(Uri uri, CancellationToken cancellationToken) =>
            _clientWebSocket.Connect(uri, cancellationToken);

        public WebSocketState State => _clientWebSocket.State;

        public Task<ReceiveResult> Receive(CancellationToken cancellationToken) =>
            _clientWebSocket.Receive(cancellationToken);

        public async Task Close(CancellationToken cancellationToken)
        {
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.Close(cancellationToken).ConfigureAwait(false);
            }
            _clientWebSocket.Dispose();
            await _receiveTaskData.DisposeAsync().ConfigureAwait(false);
            await _monitorTaskData.DisposeAsync().ConfigureAwait(false);
        }
    }
}