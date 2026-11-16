using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class SqliteSeedStateStore : ISeedStateStore
{
    public Task<Result<Option<string>, ClassificationFailure>> GetAppliedVersionAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(GetAppliedVersionAsync));

    public Task<Result<Unit, ClassificationFailure>> SetAppliedVersionAsync(string version, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(SetAppliedVersionAsync)} {version}");
}
