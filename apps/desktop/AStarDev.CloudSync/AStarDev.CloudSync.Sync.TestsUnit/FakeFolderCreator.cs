using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeFolderCreator : IRemoteFolderCreator
{
    private readonly Lock gate = new();
    private readonly List<(Option<RemoteItemId> Parent, string Name)> calls = [];

    public Func<string, Result<RemoteFolder, ProviderError>>? Respond { get; set; }

    public IReadOnlyList<(Option<RemoteItemId> Parent, string Name)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Task<Result<RemoteFolder, ProviderError>> EnsureFolderAsync(AccountId accountId, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add((parent, name));

        return Task.FromResult(Respond?.Invoke(name) ?? Result.Success<RemoteFolder, ProviderError>(RemoteFolderFactory.Create(UploadPlanFixture.Id($"folder-{name}"), name, parent.Match(id => id, () => UploadPlanFixture.Id("root")), RemoteFolderChildrenFactory.CreateNoChildFolders())));
    }
}
