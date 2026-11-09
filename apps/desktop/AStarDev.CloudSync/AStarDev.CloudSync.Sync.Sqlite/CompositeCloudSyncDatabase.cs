using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Migrates the accounts database first and the sync tables after it. The sync migration only runs when the accounts one succeeded.</summary>
/// <param name="first">The migration that runs first.</param>
/// <param name="second">The migration that runs when the first succeeded.</param>
internal sealed class CompositeCloudSyncDatabase(ICloudSyncDatabase first, ICloudSyncDatabase second) : ICloudSyncDatabase
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) =>
        first.MigrateAsync(cancellationToken).BindAsync(_ => second.MigrateAsync(cancellationToken));
}
