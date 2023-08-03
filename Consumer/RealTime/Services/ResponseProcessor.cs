using Consumer.RealTime.Extensions;
using Consumer.RealTime.Models;
using Consumer.RealTime.Models.Dtos;

namespace Consumer.RealTime.Services;

public sealed class ResponseProcessor<T>
    where T : Response
{
    private readonly ConcurrentCollectionWrapper<T> _responses = new();

    public async Task<TResult> SendAndReceive<TResult, TParam>(IClientWebSocketWrapper clientWebSocketWrapper, CancellationTokenSources tokenSources, TParam param, Func<string, TParam, Request> requestProvider, Func<T, TResult> getOkMessage,
        Func<TResult> getCancelMessage)
    {
        var requestId = Guid.NewGuid().ToString();
        var request = requestProvider(requestId, param);
        await clientWebSocketWrapper.Send(request, tokenSources.LinkedTokenSourceToken).ConfigureAwait(false);

        return Wrappers.HandleResponse((_responses, requestId),
            static p => p._responses.TryGetAndRemove(p.requestId),
            getOkMessage,
            getCancelMessage,
            tokenSources);
    }

    public void Add(T response) => 
        _responses.Add(response);

    public void Clear() => 
        _responses.Clear();
}