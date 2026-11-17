using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Runs one migration at a time (#1854). The overview and the classification startup both migrate at startup; a caller that arrives while another migrates waits for it and then runs its own, which finds nothing to do.</summary>
/// <param name="database">The migration that is serialised.</param>
public sealed class SerialCloudSyncDatabase(ICloudSyncDatabase database) : ICloudSyncDatabase, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var migration = MigrateOnceAsync(cancellationToken);
        await ((Task)migration).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _ = gate.Release();

        return await migration.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => gate.Dispose();

    private async Task<Result<Unit, ErrorResponse>> MigrateOnceAsync(CancellationToken cancellationToken) => await database.MigrateAsync(cancellationToken).ConfigureAwait(false);
}
