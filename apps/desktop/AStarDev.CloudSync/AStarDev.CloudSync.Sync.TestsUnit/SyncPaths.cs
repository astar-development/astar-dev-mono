using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class SyncPaths
{
    public static RelativeSyncPath Of(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));
}
