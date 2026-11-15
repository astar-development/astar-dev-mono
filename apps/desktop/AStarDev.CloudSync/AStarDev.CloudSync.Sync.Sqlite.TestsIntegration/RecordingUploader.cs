using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class RecordingUploader : IRemoteFileUploader
{
    private readonly Lock gate = new();
    private readonly List<string> uploaded = [];

    public IReadOnlyList<string> Uploaded
    {
        get
        {
            lock (gate)
                return [.. uploaded];
        }
    }

    public Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        lock (gate)
            uploaded.Add(source.Name);

        return Task.FromResult(Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(Id($"id-{source.Name}"), new ContentTag($"c-{source.Name}"), new EntityTag($"e-{source.Name}"), source.Length, new ContentHash($"h-{source.Name}"))));
    }
}
