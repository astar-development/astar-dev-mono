namespace AStarDev.ScraperPlaying.Operations;

public sealed class OperationCoordinator : IDisposable
{
    private readonly Lock gate = new();
    private CancellationTokenSource? cancellationTokenSource;

    public bool IsOperationRunning
    {
        get
        {
            lock (gate) return cancellationTokenSource is not null;
        }
    }

    public event EventHandler? StateChanged;

    public bool TryStart(out CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (cancellationTokenSource is not null)
            {
                cancellationToken = default;

                return false;
            }

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Cancel()
    {
        lock (gate) cancellationTokenSource?.Cancel();
    }

    public void Complete()
    {
        lock (gate)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (gate)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
}
