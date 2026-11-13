using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class InMemorySyncOutcomes : ISyncOutcomeReader, ISyncOutcomeWriter
{
    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, SyncStatus> statuses = [];
    private readonly List<(AccountId AccountId, SyncStatus Status)> saved = [];

    public event EventHandler? Changed;

    public IReadOnlyList<(AccountId AccountId, SyncStatus Status)> Saved
    {
        get
        {
            lock (gate)
                return [.. saved];
        }
    }

    public Option<ErrorResponse> SaveFails { get; set; } = Option.None<ErrorResponse>();

    public Option<ErrorResponse> ReadFails { get; set; } = Option.None<ErrorResponse>();

    public Action<AccountId> OnSave { get; set; } = _ => { };

    public Task<Result<IReadOnlyDictionary<AccountId, SyncStatus>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return Task.FromResult(ReadFails.Match(
                Result.Failure<IReadOnlyDictionary<AccountId, SyncStatus>, ErrorResponse>,
                () => Result.Success<IReadOnlyDictionary<AccountId, SyncStatus>, ErrorResponse>(new Dictionary<AccountId, SyncStatus>(statuses))));
        }
    }

    public Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, SyncStatus status, CancellationToken cancellationToken)
    {
        OnSave(accountId);
        var outcome = SaveFails.Match(Result.Failure<Unit, ErrorResponse>, () => Store(accountId, status));
        Changed?.Invoke(this, EventArgs.Empty);

        return Task.FromResult(outcome);
    }

    private Result<Unit, ErrorResponse> Store(AccountId accountId, SyncStatus status)
    {
        lock (gate)
        {
            statuses[accountId] = status;
            saved.Add((accountId, status));
        }

        return Result.Success<Unit, ErrorResponse>(Unit.Instance);
    }
}
