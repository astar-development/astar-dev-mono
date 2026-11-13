using System.IO.Abstractions;
using System.Security.Cryptography;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class Sha256ContentHasher : IContentHasher
{
    /// <inheritdoc />
    public Task<Result<ContentHash, LocalScanFailure>> HashAsync(IFileInfo file, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ComputeAsync(file, cancellationToken), cancellationToken)
            .MatchAsync(
                hex => ContentHashFactory.Create(hex).Match(Result.Success<ContentHash, LocalScanFailure>, error => Result.Failure<ContentHash, LocalScanFailure>(LocalScanFailureFactory.CreateUnexpected(error))),
                exception => Result.Failure<ContentHash, LocalScanFailure>(FailureOf(exception)));

    private static LocalScanFailure FailureOf(Exception exception) => exception is IOException or UnauthorizedAccessException
        ? LocalScanFailureFactory.CreateUnreadable()
        : LocalScanFailureFactory.CreateUnexpected(new ErrorResponse(exception.GetType().Name));

    private static async Task<string> ComputeAsync(IFileInfo file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenRead();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexStringLower(hash);
    }
}
