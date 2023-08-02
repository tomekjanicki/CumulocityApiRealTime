using Consumer.RealTime.Extensions;
using OneOf;

namespace Consumer.RealTime.Models;

public sealed class HeartbeatResponse : Response
{
    public Advice? Advice { get; init; } = new();

    public static HeartbeatResponse GetRequiredResponse(byte[] message)
        => GetRequiredResponse<HeartbeatResponse>(message);

    public OneOf<Advice, Error> ToResult()
    {
        if (!Successful)
        {
            return Error.GetError(Error.IsTransient());
        }

        return Advice is null ? "Advice is null.".GetError(false) : Advice;
    }
}