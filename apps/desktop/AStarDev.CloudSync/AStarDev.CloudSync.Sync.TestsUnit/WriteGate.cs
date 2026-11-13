namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class WriteGate
{
    private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Entered => entered.Task;

    public void Release() => released.TrySetResult();

    public Task PassAsync()
    {
        entered.TrySetResult();

        return released.Task;
    }
}
