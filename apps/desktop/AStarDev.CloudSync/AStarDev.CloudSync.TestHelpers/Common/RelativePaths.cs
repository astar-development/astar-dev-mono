using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class RelativePaths
{
    public static LocalRelativePath Of(string value) => LocalRelativePathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));
}
