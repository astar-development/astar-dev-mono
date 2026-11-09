using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public int Invocations { get; private set; }

    public Task InvokeAsync(Action action)
    {
        Invocations++;
        action();

        return Task.CompletedTask;
    }
}
