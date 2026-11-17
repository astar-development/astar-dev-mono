namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class FixedFileSystemCaseRule(StringComparison comparison) : IFileSystemCaseRule
{
    public StringComparison Comparison => comparison;
}
