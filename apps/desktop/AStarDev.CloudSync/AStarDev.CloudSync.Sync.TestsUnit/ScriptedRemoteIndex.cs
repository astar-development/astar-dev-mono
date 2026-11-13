using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedRemoteIndex : IRemoteIndexReader
{
    private static readonly DateTimeOffset Stamp = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly List<RemoteItemSnapshot> items = [];

    public Option<ChangeCursor> Cursor { get; set; } = Option.Some(RemoteIds.Cursor("cursor"));

    public bool Complete { get; set; } = true;

    public bool Fails { get; set; }

    public void Folder(string path)
    {
        var parent = Option.None<RemoteItemId>();
        var built = string.Empty;
        foreach (var name in path.Split('/'))
        {
            built = built.Length == 0 ? name : $"{built}/{name}";
            var existing = items.FirstOrDefault(item => item.ParentId.Equals(parent) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = RemoteItemSnapshotFactory.Create(RemoteIds.Of($"remote-{built.ToUpperInvariant()}"), parent, name, RemoteItemKindFactory.CreateFolder(), Stamp, RemoteIds.ETag("e"));
                items.Add(existing);
            }

            parent = Option.Some(existing.Id);
        }
    }

    public Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Outcome(Cursor));

    public Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Outcome(Complete));

    public Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) =>
        Task.FromResult(Outcome<IReadOnlyList<RemoteItemSnapshot>>([.. items.Where(item => item.ParentId.Equals(parent))]));

    public Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    private Result<T, ErrorResponse> Outcome<T>(T value) => Fails ? Result.Failure<T, ErrorResponse>(new ErrorResponse("locked")) : Result.Success<T, ErrorResponse>(value);
}
