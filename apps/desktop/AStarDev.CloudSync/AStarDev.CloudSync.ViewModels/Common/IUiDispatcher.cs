namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Runs work on the UI thread, so view models can be updated from background threads.</summary>
public interface IUiDispatcher
{
    /// <summary>Runs <paramref name="action"/> on the UI thread.</summary>
    /// <param name="action">The work to run.</param>
    /// <returns>A task that completes once the action has run.</returns>
    Task InvokeAsync(Action action);
}
