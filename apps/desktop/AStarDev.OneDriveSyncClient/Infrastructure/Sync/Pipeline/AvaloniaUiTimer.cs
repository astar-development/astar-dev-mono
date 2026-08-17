using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <summary>
/// Production implementation that fires on the Avalonia UI thread via <see cref="DispatcherTimer"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AvaloniaUiTimer : IUiTimer, IDisposable
{
    private DispatcherTimer? timer;
    private bool disposed;

    /// <inheritdoc />
    public void Start(TimeSpan interval, Action callback)
    {
        timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (_, _) => callback();
        timer.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            timer?.Stop();
    }
}
