using System.Net.WebSockets;
using Consumer.RealTime.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.RealTime.Services;

public static class ReceiveHandler
{
    public sealed class Dependencies<TParam>
    {
        public Dependencies(ILogger logger, IClientWebSocketWrapper? clientWebSocketWrapper, ConcurrentCollectionWrapper<HandShakeResponse> handShakeResponses, ConcurrentCollectionWrapper<SubscribeResponse> subscribeResponses, ConcurrentCollectionWrapper<UnsubscribeResponse> unsubscribeResponses, TimeSpan minimalDelay, IDataFeedHandler dataFeedHandler, Func<HeartbeatResponse, TParam, CancellationToken, Task> heartBeatHandler, TParam param)
        {
            Logger = logger;
            ClientWebSocketWrapper = clientWebSocketWrapper;
            HandShakeResponses = handShakeResponses;
            SubscribeResponses = subscribeResponses;
            UnsubscribeResponses = unsubscribeResponses;
            MinimalDelay = minimalDelay;
            DataFeedHandler = dataFeedHandler;
            HeartBeatHandler = heartBeatHandler;
            Param = param;
        }

        public ILogger Logger { get; }

        public IClientWebSocketWrapper? ClientWebSocketWrapper { get; }

        public ConcurrentCollectionWrapper<HandShakeResponse> HandShakeResponses { get; }

        public ConcurrentCollectionWrapper<SubscribeResponse> SubscribeResponses { get; }

        public ConcurrentCollectionWrapper<UnsubscribeResponse> UnsubscribeResponses { get; }

        public TimeSpan MinimalDelay { get; }

        public IDataFeedHandler DataFeedHandler { get; }

        public Func<HeartbeatResponse, TParam, CancellationToken, Task> HeartBeatHandler { get; }

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
                var status = clientWebSocket.State;
                if (status != WebSocketState.Open)
                {
                    await Task.Delay(dependencies.MinimalDelay, cancellationToken).ConfigureAwait(false);

                    continue;
                }
                var result = await clientWebSocket.Receive(cancellationToken).ConfigureAwait(false);
                if (result.Close)
                {
                    break;
                }
                var channelResponse = ChannelResponse.GetResponse(result.Data);
                if (channelResponse is not null)
                {
                    switch (channelResponse.Channel)
                    {
                        case HandShakeRequest.Name:
                            dependencies.HandShakeResponses.Add(HandShakeResponse.GetRequiredResponse(result.Data));

                            continue;
                        case SubscribeRequest.Name:
                            dependencies.SubscribeResponses.Add(SubscribeResponse.GetRequiredResponse(result.Data));

                            continue;
                        case UnsubscribeRequest.Name:
                            dependencies.UnsubscribeResponses.Add(UnsubscribeResponse.GetRequiredResponse(result.Data));

                            continue;
                        case HeartbeatRequest.Name:
                            await dependencies.HeartBeatHandler(HeartbeatResponse.GetRequiredResponse(result.Data), dependencies.Param, cancellationToken).ConfigureAwait(false);

                            continue;
                    }
                }
                await dependencies.DataFeedHandler.Handle(result.Data, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                dependencies.Logger.LogDebug(e, "Generic error in ReceiveHandler.");
            }
        }
    }
}