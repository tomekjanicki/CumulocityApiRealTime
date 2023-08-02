using System.Net.WebSockets;
using Consumer.RealTime.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.RealTime.Services;

public static class MonitorHandler
{
    public sealed class Dependencies<TParam>
    {
        public Dependencies(ILogger logger, IClientWebSocketWrapper? clientWebSocketWrapper, TimeSpan minimalDelay, TimeSpan monitorDelay, Func<TParam, Advice> adviceProvider, Func<TParam, bool> fullReconnectProvider, HeartBeatTimes heartBeatTimes, Func<DateTime> nowProvider, Func<TParam, CancellationToken, Task> handleAbortedOrClosedOrFullReconnectOrTimeOuted, TParam param)
        {
            Logger = logger;
            ClientWebSocketWrapper = clientWebSocketWrapper;
            MinimalDelay = minimalDelay;
            MonitorDelay = monitorDelay;
            AdviceProvider = adviceProvider;
            FullReconnectProvider = fullReconnectProvider;
            HeartBeatTimes = heartBeatTimes;
            NowProvider = nowProvider;
            HandleAbortedOrClosedOrFullReconnectOrTimeOuted = handleAbortedOrClosedOrFullReconnectOrTimeOuted;
            Param = param;
        }

        public ILogger Logger { get; }

        public IClientWebSocketWrapper? ClientWebSocketWrapper { get; }

        public TimeSpan MinimalDelay { get; }

        public TimeSpan MonitorDelay { get; }

        public Func<TParam, Advice> AdviceProvider { get; }

        public Func<TParam, bool> FullReconnectProvider { get; }

        public HeartBeatTimes HeartBeatTimes { get; }

        public Func<DateTime> NowProvider { get; }

        public Func<TParam, CancellationToken, Task> HandleAbortedOrClosedOrFullReconnectOrTimeOuted { get; }

        public TParam Param { get; }
    }

    public static async Task Handler<TParam>(Dependencies<TParam> dependencies, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientWebSocket = dependencies.ClientWebSocketWrapper;
                if (clientWebSocket is null)
                {
                    await Task.Delay(dependencies.MinimalDelay, cancellationToken).ConfigureAwait(false);

                    continue;
                }
                var state = clientWebSocket.State;
                var advice = dependencies.AdviceProvider(dependencies.Param);
                var timeOuted = advice.IsTimeOuted(dependencies.HeartBeatTimes, dependencies.NowProvider(), dependencies.MonitorDelay);
                var fullReconnect = dependencies.FullReconnectProvider(dependencies.Param);
                dependencies.Logger.LogDebug("ClientWebSocketState: {State} FullReconnect: {FullReconnect}, HeartBeats: {HeartBeats}, TimeOuted: {TimeOuted}.", state, fullReconnect, dependencies.HeartBeatTimes, timeOuted);
                if (state is WebSocketState.Aborted or WebSocketState.Closed || fullReconnect || timeOuted)
                {
                    await dependencies.HandleAbortedOrClosedOrFullReconnectOrTimeOuted(dependencies.Param, cancellationToken).ConfigureAwait(false);
                }
                await Task.Delay(dependencies.MonitorDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                dependencies.Logger.LogDebug(e, "Generic error in MonitorHandler.");
            }
        }
    }
}