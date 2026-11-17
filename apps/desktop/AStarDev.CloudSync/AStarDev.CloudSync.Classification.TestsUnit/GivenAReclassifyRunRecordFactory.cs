using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAReclassifyRunRecordFactory
{
    [Fact]
    public void when_the_engine_version_is_missing_then_it_becomes_empty_and_never_matches_the_current_one()
    {
        var run = ReclassifyRunRecordFactory.Create(null, Option.None<string>(), DateTimeOffset.UnixEpoch);

        (run.EngineVersion, run.EngineVersion == ClassificationEngineVersion.Current).ShouldBe((string.Empty, false));
    }
}
