using System.Collections.Concurrent;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

internal sealed class SingleThreadContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = [];
    private readonly Thread thread;

    public SingleThreadContext()
    {
        thread = new Thread(Pump) { IsBackground = true };
        thread.Start();
    }

    public int ThreadId => thread.ManagedThreadId;

    public override void Post(SendOrPostCallback d, object? state) => queue.Add((d, state));

    public Task RunAsync(Func<Task> work)
    {
        var done = new TaskCompletionSource();
        Post(async _ =>
        {
            var running = work();
            await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            if (running.IsFaulted)
                done.SetException(running.Exception!.InnerExceptions);
            else
                done.SetResult();
        }, null);

        return done.Task;
    }

    public void Dispose() => queue.CompleteAdding();

    private void Pump()
    {
        SetSynchronizationContext(this);

        foreach (var (callback, state) in queue.GetConsumingEnumerable())
            callback(state);
    }
}
