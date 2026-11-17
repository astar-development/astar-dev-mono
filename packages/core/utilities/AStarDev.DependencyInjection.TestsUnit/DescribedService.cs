namespace AStarDev.DependencyInjection.TestsUnit;

internal sealed class DescribedService(TimeProvider clock) : IDescribedService
{
    public TimeProvider Clock { get; } = clock;
}
