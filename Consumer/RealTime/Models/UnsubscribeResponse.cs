namespace Consumer.RealTime.Models;

public sealed class UnsubscribeResponse : Response
{
    public Error? ToResult() => GetNullableError();
    public static UnsubscribeResponse GetRequiredResponse(byte[] message)
        => GetRequiredResponse<UnsubscribeResponse>(message);
}