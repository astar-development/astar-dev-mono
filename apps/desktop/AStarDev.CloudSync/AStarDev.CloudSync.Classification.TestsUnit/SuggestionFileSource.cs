using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class SuggestionFileSource(IReadOnlyList<string> paths, bool failAfterFirst = false) : IClassifiableFileSource
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.NewGuid());

    public async IAsyncEnumerable<ClassifiableFile> StreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return ClassifiableFileFactory.Create(Account, ClassificationTestData.PathOf(path));

            if (failAfterFirst)
            {
                throw new InvalidOperationException("read failed");
            }
        }
    }
}
