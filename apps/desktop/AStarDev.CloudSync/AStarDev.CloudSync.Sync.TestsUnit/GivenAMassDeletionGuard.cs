namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAMassDeletionGuard
{
    [Theory]
    [InlineData(200, 51)]
    [InlineData(1000, 251)]
    [InlineData(60, 60)]
    public void when_more_than_a_quarter_and_at_least_fifty_items_are_missing_then_the_deletions_are_held(int baselineItems, int missingItems)
    {
        var verdict = MassDeletionGuard.Evaluate(baselineItems, missingItems);

        verdict.ShouldBe(MassDeletionVerdictFactory.CreateHeld());
    }

    [Theory]
    [InlineData(200, 50)]
    [InlineData(1000, 250)]
    [InlineData(100, 49)]
    [InlineData(40, 40)]
    [InlineData(0, 0)]
    [InlineData(-5, -1)]
    [InlineData(500, 0)]
    public void when_the_missing_items_stay_within_the_limits_then_the_deletions_are_allowed(int baselineItems, int missingItems)
    {
        var verdict = MassDeletionGuard.Evaluate(baselineItems, missingItems);

        verdict.ShouldBe(MassDeletionVerdictFactory.CreateAllowed());
    }
}
