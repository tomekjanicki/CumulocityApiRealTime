using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public interface ISubscriptionService
{
    Task<OneOf<string, ApiError>> Create(BaseSubscription subscription, CancellationToken token = default);
    Task<ApiError?> Delete(string id, CancellationToken token = default);
}