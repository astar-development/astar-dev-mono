using AStar.Dev.OneDriveSync.Features.Dashboard;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Dashboard;

public sealed class GivenAnAccountCardViewModel
{
    private static AccountCardViewModel BuildCard()
        => new("id-1", "Alice", false, null, false, false, (_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask);

    [Fact]
    public void when_eta_seconds_is_zero_then_eta_display_is_empty()
    {
        var sut = BuildCard();

        sut.EtaDisplay.ShouldBeEmpty();
    }

    [Fact]
    public void when_eta_seconds_is_less_than_60_then_eta_display_shows_seconds()
    {
        var sut = BuildCard();

        sut.EtaSeconds = 45;

        sut.EtaDisplay.ShouldBe("ETA 45s");
    }

    [Fact]
    public void when_eta_seconds_is_60_or_more_then_eta_display_shows_minutes_and_seconds()
    {
        var sut = BuildCard();

        sut.EtaSeconds = 185;

        sut.EtaDisplay.ShouldBe("ETA 3m 5s");
    }
}
