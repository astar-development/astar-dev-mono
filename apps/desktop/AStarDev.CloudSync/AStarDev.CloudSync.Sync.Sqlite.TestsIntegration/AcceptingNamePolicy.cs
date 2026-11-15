using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class AcceptingNamePolicy : IRemoteNamePolicy
{
    public Result<Unit, InvalidRemoteName> Check(string name, RelativeSyncPath path, long size) => Result.Success<Unit, InvalidRemoteName>(Unit.Instance);
}
