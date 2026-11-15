using AStarDev.CloudSync.TestHelpers.Common;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenTheRecoveryBlockerFactory
{
    [Fact]
    public void when_a_blocker_case_is_declared_then_the_factory_makes_it()
    {
        RecoveryBlockerCases.All().Select(blocker => blocker.GetType()).Distinct().Count().ShouldBe(RecoveryBlockerCases.Declared());
    }
}
