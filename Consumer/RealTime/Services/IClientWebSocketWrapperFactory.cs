using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Consumer.RealTime.Services;

public interface IClientWebSocketWrapperFactory
{
    IClientWebSocketWrapper GetNewInstance<TParam>(ILogger logger, Uri uri, TParam param, Func<byte[], TParam, CancellationToken, Task> dataHandler,
        Func<WebSocketState, TParam, CancellationToken, Task> monitorHandler);

}