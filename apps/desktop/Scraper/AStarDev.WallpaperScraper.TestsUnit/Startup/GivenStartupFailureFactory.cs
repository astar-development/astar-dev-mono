using AStarDev.WallpaperScraper.Startup;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenStartupFailureFactory
{
    [Fact]
    public void when_created_with_a_step_name_then_the_step_is_preserved()
    {
        var exception = new InvalidOperationException("boom");

        var sut = StartupFailureFactory.Create("Database migration", exception);

        sut.Step.ShouldBe("Database migration");
    }

    [Fact]
    public void when_created_with_a_step_name_then_the_exception_is_preserved()
    {
        var exception = new InvalidOperationException("boom");

        var sut = StartupFailureFactory.Create("Database migration", exception);

        sut.Exception.ShouldBe(exception);
    }

    [Fact]
    public void when_created_with_a_padded_step_name_then_it_is_trimmed()
    {
        var sut = StartupFailureFactory.Create("  Database migration  ", new InvalidOperationException("boom"));

        sut.Step.ShouldBe("Database migration");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_step_name_then_it_normalizes_to_unknown_startup_step(string step)
    {
        var sut = StartupFailureFactory.Create(step, new InvalidOperationException("boom"));

        sut.Step.ShouldBe("Unknown startup step");
    }
}
