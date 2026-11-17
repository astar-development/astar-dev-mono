using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public sealed class QueuedUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> queue = new();

    public int Pending => queue.Count;

    public Task InvokeAsync(Action action)
    {
        queue.Enqueue(action);

        return Task.CompletedTask;
    }

    public void RunQueued()
    {
        while (queue.Count > 0)
            queue.Dequeue()();
    }
}
