using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Production implementation that posts work via <see cref="Dispatcher.UIThread"/>.
/// </summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
