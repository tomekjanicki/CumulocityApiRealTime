using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.RealTime.Services;

public sealed class ClientWebSocketWrapperFactory : IClientWebSocketWrapperFactory
{
    private readonly TimeSpan _monitorDelay;

    public ClientWebSocketWrapperFactory(IOptions<ConfigurationSettings> options) => 
        _monitorDelay = options.Value.WebSocketClientMonitorInterval;

    public IClientWebSocketWrapper GetNewInstance<TParam>(ILogger logger, Uri uri, TParam param, Func<byte[], TParam, CancellationToken, Task> dataHandler,
        Func<WebSocketState, TParam, CancellationToken, Task> monitorHandler) => new ClientWebSocketWrapper<TParam>(uri, logger, _monitorDelay, dataHandler, monitorHandler, param);
}