using AStarDev.WallpaperScraper.Startup;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenStartupDiagnostics
{
    [Fact]
    public void when_constructed_then_failures_is_empty() =>
        new StartupDiagnostics().Failures.ShouldBeEmpty();

    [Fact]
    public void when_a_failure_is_recorded_then_it_appears_in_failures()
    {
        var sut = new StartupDiagnostics();
        var failure = StartupFailureFactory.Create("Database migration", new InvalidOperationException("boom"));

        sut.RecordFailure(failure);

        sut.Failures.ShouldContain(failure);
    }

    [Fact]
    public void when_multiple_failures_are_recorded_then_they_appear_in_the_order_recorded()
    {
        var sut = new StartupDiagnostics();
        var first = StartupFailureFactory.Create("Application directories", new InvalidOperationException("first"));
        var second = StartupFailureFactory.Create("Database migration", new InvalidOperationException("second"));

        sut.RecordFailure(first);
        sut.RecordFailure(second);

        sut.Failures.ShouldBe([first, second]);
    }
}
