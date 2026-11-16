using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyFakeFileSource(IReadOnlyList<ClassifiableFile> files) : IClassifiableFileSource
{
    internal int FailAfter { get; init; } = int.MaxValue;

    internal Task? BlockUntil { get; init; }

    internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async IAsyncEnumerable<ClassifiableFile> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = Started.TrySetResult();
        if (BlockUntil is not null)
            await BlockUntil.WaitAsync(cancellationToken);

        var index = 0;
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index++ == FailAfter)
                throw new InvalidOperationException("The stream failed.");

            yield return file;
        }

        await Task.CompletedTask;
    }

    internal static ReclassifyFakeFileSource Numbered(AccountId account, int count) =>
        new([.. Enumerable.Range(0, count).Select(index => ClassifiableFileFactory.Create(account, ClassificationTestData.PathOf($"Holiday/file-{index:D6}.jpg")))]);
}
