using Client.Com.Cumulocity.Client.Api;
using Client.Com.Cumulocity.Client.Model;
using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public sealed class TokenService : ITokenService
{
    private readonly IHttpClientFactory _clientFactory;

    public TokenService(IHttpClientFactory clientFactory) =>
        _clientFactory = clientFactory;

    public Task<OneOf<string, ApiError>> CreateToken(TokenClaim tokenClaim, CancellationToken token = default) =>
        Wrappers.HandleWithException(_clientFactory, tokenClaim,
            static (client, p, token) => CreateTokenInt(client, p, token), token);

    public Task<ApiError?> Unsubscribe(string token, CancellationToken cancellationToken = default) =>
        Wrappers.HandleWithException(_clientFactory, token, static (client, p, cancellationToken) => UnsubscribeInt(client, p, cancellationToken), cancellationToken);

    private static async Task<OneOf<string, ApiError>> CreateTokenInt(HttpClient client, TokenClaim tokenClaim, CancellationToken token = default)
    {
        var api = new TokensApi(client);
        var claims = new NotificationTokenClaims(tokenClaim.Subscriber, tokenClaim.Subscription)
        {
            ExpiresInMinutes = tokenClaim.ExpiresInMinutes,
            Signed = tokenClaim.Signed,
            NonPersistent = tokenClaim.NonPersistent,
            Shared = tokenClaim.Shared
        };
        var result = await api.CreateToken(claims, cToken: token).ConfigureAwait(false);
        var resultToken = result?.Token;

        return resultToken is null ? Constants.NullResultApiError : resultToken;
    }

    private static async Task<ApiError?> UnsubscribeInt(HttpClient client, string token, CancellationToken cancellationToken = default)
    {
        var api = new TokensApi(client);
        var result = await api.UnsubscribeSubscriber(token: token, cToken: cancellationToken).ConfigureAwait(false);
        var pResult = result?.PResult;

        return pResult is null ? Constants.NullResultApiError : null;
    }
}