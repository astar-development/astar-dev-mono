namespace AStarDev.CloudSync.Sync;

/// <summary>The scope returned when a logger does not support scopes.</summary>
internal sealed class NoLogScope : IDisposable
{
    private NoLogScope()
    {
    }

    /// <summary>The shared instance.</summary>
    public static NoLogScope Instance { get; } = new();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
