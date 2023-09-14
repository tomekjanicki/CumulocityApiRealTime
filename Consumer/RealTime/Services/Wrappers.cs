using Consumer.RealTime.Models;
using OneOf;

namespace Consumer.RealTime.Services;

public static class Wrappers
{
    public static async Task HandlerWrapper<T, TArgument>(T param, Func<TArgument?> argumentFunc, TimeSpan minimalDelay, Action<Exception, T> outerLoggerAction, Action<Exception, T> innerLoggerAction,
        Func<T, TArgument, CancellationToken, Task> jobTask, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var argument = argumentFunc();
                    if (argument is null)
                    {
                        await Task.Delay(minimalDelay, cancellationToken).ConfigureAwait(false);

                        continue;
                    }
                    await jobTask(param, argument, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    innerLoggerAction(e, param);
                }
            }
        }
        catch (Exception e)
        {
            outerLoggerAction(e, param);
        }
    }

    public static async Task<TResult> ExecutionWrapper<TResult, TParam>(TParam param, TimeSpan timeOut, Func<TParam, CancellationToken, Task<TResult>> getOkResult, Func<TResult> getTimeoutResult, CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = Helper.CreateCancellationTokenSource(cancellationToken, timeOut);
        try
        {
            return await getOkResult(param, cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return getTimeoutResult();
            }

            throw;
        }
    }

    public static TResult HandleResponse<TResult, TResponse, TParam>(TParam param, Func<TParam, TResponse?> getResponseFunc,
        Func<TResponse, TResult> getOkMessage, Func<TResult> getCancelMessage, CancellationToken token)
        where TResponse : class
    {
        while (true)
        {
            var message = getResponseFunc(param);
            if (message is not null)
            {
                return getOkMessage(message);
            }
            if (token.IsCancellationRequested)
            {
                return getCancelMessage();
            }
            token.ThrowIfCancellationRequested();
        }
    }

    public static async Task<OneOf<TResult, ApiError>> HandleWithException<TResult, TParam>(IHttpClientFactory clientFactory, TParam param, Func<HttpClient, TParam, CancellationToken, Task<OneOf<TResult, ApiError>>> func, CancellationToken token = default)
    {
        var client = clientFactory.CreateClient(Constants.ClientName);
        try
        {
            return await func(client, param, token).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            return new ApiError(e.Message, e.StatusCode);
        }
    }

    public static async Task<ApiError?> HandleWithException<TParam>(IHttpClientFactory clientFactory, TParam param, Func<HttpClient, TParam, CancellationToken, Task<ApiError?>> func, CancellationToken token = default)
    {
        var client = clientFactory.CreateClient(Constants.ClientName);
        try
        {
            return await func(client, param, token).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            return new ApiError(e.Message, e.StatusCode);
        }
    }
}