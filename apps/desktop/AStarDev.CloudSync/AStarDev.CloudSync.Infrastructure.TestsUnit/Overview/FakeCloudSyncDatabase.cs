using AStarDev.CloudSync.Core.Persistence;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.Overview;

public sealed class FakeCloudSyncDatabase : ICloudSyncDatabase
{
    public Result<Unit, ErrorResponse> Outcome { get; init; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public int Migrations { get; private set; }

    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        Migrations++;

        return Task.FromResult(Outcome);
    }
}
