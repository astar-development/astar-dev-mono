namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class SqliteClassifiableFileSource : IClassifiableFileSource
{
    public IAsyncEnumerable<ClassifiableFile> StreamAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(StreamAsync));
}
