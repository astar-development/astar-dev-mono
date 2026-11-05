using AStarDev.CloudSync.ViewModels.Common;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Common;

/// <summary>Runs work on the Avalonia UI thread.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public Task InvokeAsync(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();
}
