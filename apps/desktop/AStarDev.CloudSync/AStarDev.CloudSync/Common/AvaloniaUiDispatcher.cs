using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.Common;

/// <summary>Runs work on the Avalonia UI thread.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public Task InvokeAsync(Action action) => throw new NotImplementedException($"{action}");
}
