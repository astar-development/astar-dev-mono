using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedDownloader : IRemoteFileDownloader
{
    private readonly Lock gate = new();
    private readonly List<string> calls = [];
    private int running;
    private int mostRunning;

    public Dictionary<string, byte[]> Content { get; } = [];

    public Dictionary<string, ProviderError> Failures { get; } = [];

    public Dictionary<string, long> Truncate { get; } = [];

    public Func<string, IFileInfo, Task>? OnDownload { get; set; }

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public int MostRunning => mostRunning;

    public async Task<Result<DownloadedFile, ProviderError>> DownloadAsync(AccountId accountId, RemoteItemId fileId, IFileInfo target, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            calls.Add(fileId.Value);
            running++;
            mostRunning = Math.Max(mostRunning, running);
        }

        try
        {
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, cancellationToken);

            if (Failures.TryGetValue(fileId.Value, out var failure))
                return Result.Failure<DownloadedFile, ProviderError>(failure);

            var bytes = Content.TryGetValue(fileId.Value, out var found) ? found : [];
            var written = Truncate.TryGetValue(fileId.Value, out var length) ? bytes[..(int)length] : bytes;
            target.FileSystem.File.WriteAllBytes(target.FullName, written);
            if (OnDownload is not null)
                await OnDownload(fileId.Value, target);

            cancellationToken.ThrowIfCancellationRequested();

            return Result.Success<DownloadedFile, ProviderError>(DownloadedFileFactory.Create(fileId, RemoteIds.CTag("c-downloaded"), written.Length, Option.None<ContentHash>()));
        }
        finally
        {
            lock (gate)
                running--;
        }
    }
}
