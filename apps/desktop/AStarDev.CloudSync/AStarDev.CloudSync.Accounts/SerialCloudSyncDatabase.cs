using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Runs one migration at a time (#1854). The overview and the classification startup both migrate at startup; a caller that arrives while another migrates waits for it and then runs its own, which finds nothing to do.</summary>
/// <param name="database">The migration that is serialised.</param>
public sealed class SerialCloudSyncDatabase(ICloudSyncDatabase database) : ICloudSyncDatabase, IDisposable
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{database}{cancellationToken}");

    /// <inheritdoc />
    public void Dispose() => throw new NotImplementedException();
}
