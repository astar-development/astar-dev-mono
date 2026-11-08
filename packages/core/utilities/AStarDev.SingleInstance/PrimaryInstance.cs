namespace AStarDev.SingleInstance;

/// <summary>The running instance: holds the lock and listens for second launches until disposed.</summary>
public sealed class PrimaryInstance : IAsyncDisposable
{
    internal PrimaryInstance()
    {
    }

    /// <summary>Raised on a thread-pool thread when a second launch asks this instance to show its window.</summary>
    public event EventHandler? ActivationRequested;

    /// <summary>Stops listening, removes the socket file and releases the lock.</summary>
    public ValueTask DisposeAsync() => throw new NotImplementedException($"{ActivationRequested}");
}
