using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingRemoteIndexWriter : IRemoteIndexWriter
{
    public List<(RemoteItemId ItemId, string NewName)> Renames { get; } = [];

    public bool RenameFails { get; set; }

    public Task<Result<Unit, ErrorResponse>> RenameItemAsync(AccountId accountId, RemoteItemId itemId, string newName, CancellationToken cancellationToken)
    {
        if (RenameFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The index could not be written.")));

        Renames.Add((itemId, newName));

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<RemoteIndexDelta, ErrorResponse>> ApplyPageAsync(AccountId accountId, RemoteChangePage page, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<ResyncPassId, ErrorResponse>> BeginResyncAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<RemoteIndexDelta, ErrorResponse>> CompleteResyncAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
