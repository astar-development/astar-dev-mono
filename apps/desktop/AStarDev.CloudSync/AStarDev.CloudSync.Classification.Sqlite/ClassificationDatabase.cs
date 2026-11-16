using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class ClassificationDatabase : ICloudSyncDatabase
{
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(MigrateAsync));
}
