using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingAttentionWriter : ISyncAttentionWriter
{
    private readonly List<SyncAttentionItem> recorded = [];

    public bool Fails { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<SyncAttentionItem.HeldDeletions> Held => [.. recorded.OfType<SyncAttentionItem.HeldDeletions>()];

    public Task<Result<Unit, ErrorResponse>> RecordAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken)
    {
        Calls++;
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        recorded.AddRange(items);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
