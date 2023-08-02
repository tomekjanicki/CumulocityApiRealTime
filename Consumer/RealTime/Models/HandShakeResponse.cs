using Consumer.RealTime.Extensions;
using OneOf;

namespace Consumer.RealTime.Models;

public sealed class HandShakeResponse : Response
{
    public string? ClientId { get; init; }

    public IReadOnlyCollection<string>? SupportedConnectionTypes { get; init; }

    public static HandShakeResponse GetRequiredResponse(byte[] message)
        => GetRequiredResponse<HandShakeResponse>(message);

    public OneOf<string, Error> ToResult(string connectionType)
    {
        if (!Successful)
        {
            return Error.GetError(Error.IsTransient());
        }
        var clientId = ClientId;
        var supportedConnectionTypes = SupportedConnectionTypes;
        if (clientId is null)
        {
            return "Client id is null.".GetError(false);
        }

        return supportedConnectionTypes is null || !supportedConnectionTypes.Contains(connectionType)
            ? $"{connectionType} is not supported.".GetError(false)
            : clientId;
    }
}