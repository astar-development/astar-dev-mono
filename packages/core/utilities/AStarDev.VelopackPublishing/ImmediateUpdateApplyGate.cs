namespace AStar.Dev.Velopack.Publishing;

/// <summary>The gate for apps that can restart at any time: it never waits (#1412). It is the default, so apps that do not register their own gate behave as before.</summary>
public sealed class ImmediateUpdateApplyGate : IUpdateApplyGate
{
    /// <inheritdoc />
    public Task<IDisposable> WaitUntilSafeAsync(CancellationToken cancellationToken) => Task.FromResult<IDisposable>(new NoPermit());

    private sealed class NoPermit : IDisposable
    {
        public void Dispose()
        {
            // Nothing is held, so there is nothing to release.
        }
    }
}
