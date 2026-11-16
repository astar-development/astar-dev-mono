using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class CancellingDatabase(CancellationTokenSource source) : ICloudSyncDatabase
{
    public async Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        await source.CancelAsync();

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("migration stopped"));
    }
}
