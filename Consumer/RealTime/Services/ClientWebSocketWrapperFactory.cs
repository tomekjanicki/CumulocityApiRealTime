using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.RealTime.Services;

public sealed class ClientWebSocketWrapperFactory : IClientWebSocketWrapperFactory
{
    private readonly TimeSpan _monitorDelay;
    private readonly Uri _uri;

    public ClientWebSocketWrapperFactory(IOptions<ConfigurationSettings> options)
    {
        _monitorDelay = options.Value.WebSocketClientMonitorInterval;
        _uri = options.Value.ApiUri;
    }

    public IClientWebSocketWrapper GetNewInstance<TParam>(ILogger logger, Func<byte[], TParam, CancellationToken, Task> dataHandler,
        Func<WebSocketState, TParam, CancellationToken, Task> monitorHandler, TParam param) =>
        new ClientWebSocketWrapper<TParam>(_uri, logger, _monitorDelay, dataHandler, monitorHandler, param);
}