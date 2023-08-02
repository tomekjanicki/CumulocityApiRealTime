namespace Consumer.RealTime.Models;

public sealed class TaskData<TParam> : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _task;

    public TaskData(TParam param, Func<CancellationTokenSource, TParam, Task> task)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _task = task(_cancellationTokenSource, param);
    }

    public async Task Stop()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            await _task.ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dispose() =>
        _cancellationTokenSource.Dispose();
}