using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public sealed class Notification2Facade
{
    private readonly ITokenService _tokenService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IRealTimeWebSocketClient2 _realTimeWebSocketClient;

    public Notification2Facade(ITokenService tokenService, ISubscriptionService subscriptionService, IRealTimeWebSocketClient2 realTimeWebSocketClient)
    {
        _tokenService = tokenService;
        _subscriptionService = subscriptionService;
        _realTimeWebSocketClient = realTimeWebSocketClient;
    }

    public async Task<OneOf<ConnectionData, ApiError>> Start(BaseSubscription subscription, CancellationToken cancellationToken = default)
    {
        var createSubscriptionResult = await _subscriptionService.Create(subscription, cancellationToken).ConfigureAwait(false);
        if (createSubscriptionResult.IsT1)
        {
            return createSubscriptionResult.AsT1;
        }
        var subscriptionId = createSubscriptionResult.AsT0;
        var createTokenResult = await _tokenService.CreateToken(new TokenClaim(subscriptionId, subscription.Name), cancellationToken).ConfigureAwait(false);
        if (createTokenResult.IsT1)
        {
            return createTokenResult.AsT1;
        }
        var token = createTokenResult.AsT0;
        var connectResult = await _realTimeWebSocketClient.Connect(token, cancellationToken).ConfigureAwait(false);

        return connectResult is not null
            ? new ApiError(connectResult.Message, null)
            : new ConnectionData(subscriptionId, token);
    }

    public async Task<ApiError?> Stop(ConnectionData connectionData, CancellationToken cancellationToken = default)
    {
        await _realTimeWebSocketClient.Disconnect(cancellationToken).ConfigureAwait(false);
        var unsubscribeResult = await _tokenService.Unsubscribe(connectionData.Token, cancellationToken).ConfigureAwait(false);

        return unsubscribeResult ?? await _subscriptionService.Delete(connectionData.SubscriptionId, cancellationToken).ConfigureAwait(false);
    }
}