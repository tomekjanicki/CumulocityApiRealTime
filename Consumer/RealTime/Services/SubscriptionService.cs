using Client.Com.Cumulocity.Client.Api;
using Client.Com.Cumulocity.Client.Model;
using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly IHttpClientFactory _clientFactory;

    public SubscriptionService(IHttpClientFactory clientFactory) => 
        _clientFactory = clientFactory;

    public Task<OneOf<string, ApiError>> Create(BaseSubscription subscription, CancellationToken token = default) => 
        Wrappers.HandleWithException(_clientFactory, subscription, static (httpClient, p, cancellationToken) => CreateInt(httpClient, p, cancellationToken), token);

    public Task<ApiError?> Delete(string id, CancellationToken token = default) => 
        Wrappers.HandleWithException(_clientFactory, id, static (client, p, cancellationToken) => DeleteInt(client, p, cancellationToken), token);

    private static async Task<OneOf<string, ApiError>> CreateInt(HttpClient client, BaseSubscription subscription, CancellationToken token = default)
    {
        var api = new SubscriptionsApi(client);
        var context = subscription is TenantSubscription ? NotificationSubscription.Context.TENANT : NotificationSubscription.Context.MO;
        var request = new NotificationSubscription(context, subscription.Name);
        if (subscription is ManagedObjectSubscription managedObjectSubscription)
        {
            request.PSource = new NotificationSubscription.Source { Id = managedObjectSubscription.Id };
        }
        if (subscription.FragmentsToCopy.Count > 0)
        {
            request.FragmentsToCopy = subscription.FragmentsToCopy.ToList();
        }
        if (subscription.Filter is not null)
        {
            request.PSubscriptionFilter = new NotificationSubscription.SubscriptionFilter
            {
                TypeFilter = subscription.Filter.Type,
                Apis = subscription.Filter.Apis.ToList()
            };
        }
        var result = await api.CreateSubscription(request, cToken: token).ConfigureAwait(false);
        var id = result?.Id;

        return id is null ? Constants.NullResultApiError : id;
    }

    private static async Task<ApiError?> DeleteInt(HttpClient client, string id, CancellationToken token = default)
    {
        var api = new SubscriptionsApi(client);
        await api.DeleteSubscription(id, cToken: token).ConfigureAwait(false);

        return null;
    }
}