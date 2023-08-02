namespace Consumer.RealTime.Models;

public sealed class TaskData<TParam> : IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _task;

    public TaskData(TParam param, Func<TParam, CancellationTokenSource, Task> task)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _task = task(param, _cancellationTokenSource);
    }

    private async Task Stop()
    {
        _cancellationTokenSource.Cancel();
        await _task.ConfigureAwait(false);
        _cancellationTokenSource.Dispose();
    }

    public async ValueTask DisposeAsync() => 
        await Stop().ConfigureAwait(false);
}