using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenAWorkerCountOptionFactory
{
    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));

        return loc;
    }

    [Fact]
    public void when_create_called_then_returns_four_options()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 4);

        options.Count.ShouldBe(4);
    }

    [Fact]
    public void when_create_called_then_count_values_are_2_4_6_8()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 4);

        var counts = options.Select(option => option.Count).ToList();
        counts.ShouldContain(2);
        counts.ShouldContain(4);
        counts.ShouldContain(6);
        counts.ShouldContain(8);
    }

    [Fact]
    public void when_selected_count_is_2_then_count_2_option_is_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 2);

        options.Single(option => option.Count == 2).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_count_is_2_then_other_options_are_not_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 2);

        options.Where(option => option.Count != 2).ShouldAllBe(option => !option.IsSelected);
    }

    [Fact]
    public void when_selected_count_is_4_then_count_4_option_is_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 4);

        options.Single(option => option.Count == 4).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_count_is_6_then_count_6_option_is_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 6);

        options.Single(option => option.Count == 6).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_count_is_8_then_count_8_option_is_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 8);

        options.Single(option => option.Count == 8).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_count_matches_no_option_then_no_option_is_selected()
    {
        var options = WorkerCountOptionFactory.Create(BuildLocalizationService(), 99);

        options.ShouldAllBe(option => !option.IsSelected);
    }
}
