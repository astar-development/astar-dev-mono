using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenAStatTileViewModel
{
    [Fact]
    public void when_the_tone_is_default_then_neither_warn_nor_accent_is_selected()
    {
        var tile = new StatTileViewModel(() => "4", "Stats.Accounts", StatTileToneFactory.CreateDefault(), TestData.CreateLocalization());

        tile.Tone.ShouldBeOfType<StatTileTone.Default>();
        tile.IsWarn.ShouldBeFalse();
        tile.IsAccent.ShouldBeFalse();
    }

    [Fact]
    public void when_the_tone_is_warn_then_only_warn_is_selected()
    {
        var tile = new StatTileViewModel(() => "2", "Stats.Conflicts", StatTileToneFactory.CreateWarn(), TestData.CreateLocalization());

        tile.Tone.ShouldBeOfType<StatTileTone.Warn>();
        tile.IsWarn.ShouldBeTrue();
        tile.IsAccent.ShouldBeFalse();
    }

    [Fact]
    public void when_the_tone_is_accent_then_only_accent_is_selected()
    {
        var tile = new StatTileViewModel(() => "Syncing", "Stats.OverallStatus", StatTileToneFactory.CreateAccent(), TestData.CreateLocalization());

        tile.Tone.ShouldBeOfType<StatTileTone.Accent>();
        tile.IsWarn.ShouldBeFalse();
        tile.IsAccent.ShouldBeTrue();
    }
}
