using Consumer.RealTime.Models;

namespace Consumer.RealTime.Services;

public static class Wrappers
{
    public static async Task<TResult> ExecutionWrapper<TResult, TParam>(TParam param, TimeSpan timeOut, Func<TParam, CancellationTokenSources, Task<TResult>> getOkResult, Func<TResult> getTimeoutResult, CancellationToken cancellationToken)
    {
        using var tokenSources = CancellationTokenSources.Create(timeOut, cancellationToken);
        try
        {
            return await getOkResult(param, tokenSources).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            if (tokenSources.InternalTokenSourceIsCancellationRequested)
            {
                return getTimeoutResult();
            }

            throw;
        }
    }

    public static TResult HandleResponse<TResult, TResponse, TParam>(TParam param, Func<TParam, TResponse?> getResponseFunc,
        Func<TResponse, TResult> getOkMessage, Func<TResult> getCancelMessage, CancellationTokenSources tokenSources)
        where TResponse : class
    {
        while (true)
        {
            var message = getResponseFunc(param);
            if (message is not null)
            {
                return getOkMessage(message);
            }
            if (tokenSources.InternalTokenSourceIsCancellationRequested)
            {
                return getCancelMessage();
            }
            tokenSources.LinkedTokenSourceToken.ThrowIfCancellationRequested();
        }
    }
}