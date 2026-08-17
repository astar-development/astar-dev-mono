using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;
namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <summary>
/// Production implementation that posts work via <see cref="Dispatcher.UIThread"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
