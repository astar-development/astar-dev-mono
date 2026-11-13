using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="timeProvider">Times the wait between retries.</param>
internal sealed class LocalFileOperations(TimeProvider timeProvider) : ILocalFileOperations
{
    private const int MaxAttempts = 3;
    private const int DiskFullWindows = unchecked((int)0x80070070);
    private const int DiskFullPosix = 28;

    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(200);

    /// <inheritdoc />
    public Task<Result<Unit, LocalOperationFailure>> CreateFolderAsync(IDirectoryInfo folder, CancellationToken cancellationToken) => WithRetryAsync(() => Do(folder.Create), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, LocalOperationFailure>> MoveAsync(IFileSystemInfo source, string targetPath, CancellationToken cancellationToken) => WithRetryAsync(() => Do(() => MoveItem(source, targetPath)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, LocalOperationFailure>> DeleteAsync(IFileSystemInfo item, CancellationToken cancellationToken) => WithRetryAsync(() => Do(() => DeleteItem(item)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, LocalOperationFailure>> ReplaceAsync(IFileInfo download, IFileInfo target, DateTimeOffset lastWriteUtc, CancellationToken cancellationToken) =>
        WithRetryAsync(() => Do(() => ReplaceFile(download, target, lastWriteUtc)), cancellationToken);

    private static Exceptional<Unit> Do(Action action) => Try.Run(() =>
    {
        action();

        return Unit.Instance;
    });

    private static void MoveItem(IFileSystemInfo source, string targetPath)
    {
        var fileSystem = source.FileSystem;
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(targetPath) ?? targetPath);
        if (source is IFileInfo)
            fileSystem.File.Move(source.FullName, targetPath, overwrite: false);
        else if (source is IDirectoryInfo)
            fileSystem.Directory.Move(source.FullName, targetPath);
    }

    private static void DeleteItem(IFileSystemInfo item)
    {
        item.Refresh();
        if (item.Exists)
            item.Delete();
    }

    private static void ReplaceFile(IFileInfo download, IFileInfo target, DateTimeOffset lastWriteUtc)
    {
        download.FileSystem.File.Move(download.FullName, target.FullName, overwrite: true);
        target.Refresh();
        target.LastWriteTimeUtc = lastWriteUtc.UtcDateTime;
    }

    private static LocalOperationFailure Map(Exception error) => error switch
    {
        UnauthorizedAccessException => LocalOperationFailureFactory.CreateAccessDenied(),
        PathTooLongException => LocalOperationFailureFactory.CreatePathTooLong(),
        IOException io when io.HResult is DiskFullWindows or DiskFullPosix => LocalOperationFailureFactory.CreateInsufficientDiskSpace(),
        IOException => LocalOperationFailureFactory.CreateLocked(),
        _ => LocalOperationFailureFactory.CreateUnexpected(),
    };

    private async Task<Result<Unit, LocalOperationFailure>> WithRetryAsync(Func<Exceptional<Unit>> operation, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var failure = operation().Match(_ => Option.None<LocalOperationFailure>(), error => Option.Some(Map(error)));
            if (failure.Match(found => found is not LocalOperationFailure.Locked || attempt == MaxAttempts, () => true))
                return failure.Match(Result.Failure<Unit, LocalOperationFailure>, () => Result.Success<Unit, LocalOperationFailure>(Unit.Instance));

            await Task.Delay(RetryInterval, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
