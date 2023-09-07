using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public interface ITokenService
{
    Task<OneOf<string, ApiError>> CreateToken(TokenClaim tokenClaim, CancellationToken token = default);
    Task<ApiError?> Unsubscribe(string token, CancellationToken cancellationToken = default);
}