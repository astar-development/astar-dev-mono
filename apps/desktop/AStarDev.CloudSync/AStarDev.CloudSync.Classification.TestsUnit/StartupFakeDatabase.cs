using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class StartupFakeDatabase(List<string> calls) : ICloudSyncDatabase
{
    internal Result<Unit, ErrorResponse> Outcome { get; init; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        calls.Add("migrate");

        return Task.FromResult(Outcome);
    }
}
