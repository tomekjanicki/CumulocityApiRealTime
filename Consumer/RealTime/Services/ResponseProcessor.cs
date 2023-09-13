using Consumer.RealTime.Extensions;
using Consumer.RealTime.Models;
using Consumer.RealTime.Models.Dtos;

namespace Consumer.RealTime.Services;

public sealed class ResponseProcessor<T>
    where T : Response
{
    private readonly ConcurrentCollectionWrapper<T> _responses = new();

    public async Task<TResult> SendAndReceive<TResult, TParam>(IClientWebSocketWrapper clientWebSocketWrapper, TParam param, Func<string, TParam, Request> requestProvider, Func<T, TResult> getOkMessage,
        Func<TResult> getCancelMessage, CancellationToken token)
    {
        var requestId = Guid.NewGuid().ToString();
        var request = requestProvider(requestId, param);
        await clientWebSocketWrapper.Send(request, token).ConfigureAwait(false);

        return Wrappers.HandleResponse((_responses, requestId),
            static p => p._responses.TryGetAndRemove(p.requestId),
            getOkMessage,
            getCancelMessage,
            token);
    }

    public void Add(T response) => 
        _responses.Add(response);

    public void Clear() => 
        _responses.Clear();
}