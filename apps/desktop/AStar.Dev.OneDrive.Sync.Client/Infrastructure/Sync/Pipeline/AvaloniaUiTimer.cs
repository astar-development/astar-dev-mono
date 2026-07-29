using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Production implementation that fires on the Avalonia UI thread via <see cref="DispatcherTimer"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AvaloniaUiTimer : IUiTimer, IDisposable
{
    private DispatcherTimer? timer;

    /// <inheritdoc />
    public void Start(TimeSpan interval, Action callback)
    {
        timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (_, _) => callback();
        timer.Start();
    }

    /// <inheritdoc />
    public void Dispose() => timer?.Stop();
}
