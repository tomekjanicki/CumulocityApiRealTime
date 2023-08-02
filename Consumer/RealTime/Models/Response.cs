using Consumer.Extensions;
using Consumer.RealTime.Extensions;

namespace Consumer.RealTime.Models;

public abstract class Response
{
    public string Id { get; init; } = string.Empty;
    public bool Successful { get; init; }

    public string? Error { get; init; }

    protected Error? GetNullableError()
        => Successful ? null : Error.GetError(Error.IsTransient());

    protected static TResponse GetRequiredResponse<TResponse>(byte[] message)
        where TResponse : class =>
        message.GetObjectFromUtf8Bytes<IReadOnlyCollection<TResponse>>()!.First();
}