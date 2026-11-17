namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class DescribedService(TimeProvider clock) : IDescribedService
{
    public TimeProvider Clock { get; } = clock;
}
