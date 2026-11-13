using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RemoteTreeIndex : IRemoteIndexReader
{
    private static readonly DateTimeOffset Stamp = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly List<RemoteItemSnapshot> items = [];

    public void Add(RemoteItemSnapshot item) => items.Add(item);

    public void Folder(string path, string id)
    {
        var segments = path.Split('/');
        items.Add(RemoteItemSnapshotFactory.Create(UploadPlanFixture.Id(id), ParentOf(segments), segments[^1], RemoteItemKindFactory.CreateFolder(), Stamp, new EntityTag("fe")));
    }

    public void File(string path, string id, string cTag)
    {
        var segments = path.Split('/');
        items.Add(RemoteItemSnapshotFactory.Create(UploadPlanFixture.Id(id), ParentOf(segments), segments[^1], RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), new ContentTag(cTag)), Stamp, new EntityTag("e")));
    }

    public Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>([.. items.Where(item => item.ParentId.Equals(parent))]));

    public Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    private Option<RemoteItemId> ParentOf(string[] segments) => segments.Length == 1 ? Option.None<RemoteItemId>() : items.Where(item => string.Equals(item.Name, segments[^2], StringComparison.OrdinalIgnoreCase)).Select(item => Option.Some(item.Id)).FirstOrDefault(Option.None<RemoteItemId>());
}
