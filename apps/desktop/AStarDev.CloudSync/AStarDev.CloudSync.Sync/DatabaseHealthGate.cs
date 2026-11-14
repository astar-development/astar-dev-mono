namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="check">The integrity check.</param>
internal sealed class DatabaseHealthGate(IDatabaseHealthCheck check) : IDatabaseHealthGate
{
    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public DatabaseHealthStatus Status => throw new NotImplementedException($"{check} {Changed}");

    /// <inheritdoc />
    public Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");
}
