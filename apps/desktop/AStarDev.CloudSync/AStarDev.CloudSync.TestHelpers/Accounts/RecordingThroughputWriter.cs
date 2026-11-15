using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class RecordingThroughputWriter : IThroughputWriter
{
    private readonly List<TransferSample> samples = [];

    public bool Fails { get; set; }

    public IReadOnlyList<TransferSample> Samples => samples;

    public Task<Result<Unit, ErrorResponse>> AppendAsync(TransferSample sample, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        samples.Add(sample);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
