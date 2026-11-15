using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class RecordingDownloader : IRemoteFileDownloader
{
    private readonly Lock gate = new();
    private readonly List<string> downloaded = [];

    public IReadOnlyList<string> Downloaded
    {
        get
        {
            lock (gate)
                return [.. downloaded];
        }
    }

    public Task<Result<DownloadedFile, ProviderError>> DownloadAsync(AccountId accountId, RemoteItemId fileId, IFileInfo target, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        lock (gate)
            downloaded.Add(fileId.Value);

        target.FileSystem.File.WriteAllText(target.FullName, RemoteContent.Text);

        return Task.FromResult(Result.Success<DownloadedFile, ProviderError>(DownloadedFileFactory.Create(fileId, Tag("c-1"), RemoteContent.Text.Length, Option.None<ContentHash>())));
    }
}
