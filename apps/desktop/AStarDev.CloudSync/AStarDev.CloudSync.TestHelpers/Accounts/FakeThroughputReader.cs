using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeThroughputReader : IThroughputReader
{
    public Result<IReadOnlyList<TransferSample>, ErrorResponse> Samples { get; set; } = Result.Success<IReadOnlyList<TransferSample>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<TransferSample>, ErrorResponse>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) => Task.FromResult(Samples);
}
