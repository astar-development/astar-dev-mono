using AStar.Dev.OneDriveSync.Infrastructure.Shell;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Shell;

public sealed class GivenAFeatureAvailabilityService
{
    [Fact]
    public void when_a_section_has_not_been_registered_then_it_is_not_available() =>
        new FeatureAvailabilityService().IsAvailable(NavSection.Dashboard).ShouldBeFalse();

    [Theory]
    [InlineData(NavSection.Dashboard)]
    [InlineData(NavSection.Accounts)]
    [InlineData(NavSection.Activity)]
    [InlineData(NavSection.Conflicts)]
    [InlineData(NavSection.LogViewer)]
    [InlineData(NavSection.Settings)]
    [InlineData(NavSection.Help)]
    [InlineData(NavSection.About)]
    public void when_a_section_is_registered_then_it_is_available(NavSection section)
    {
        var sut = new FeatureAvailabilityService();

        sut.Register(section);

        sut.IsAvailable(section).ShouldBeTrue();
    }

    [Fact]
    public void when_only_one_section_is_registered_then_other_sections_remain_unavailable()
    {
        var sut = new FeatureAvailabilityService();
        sut.Register(NavSection.Dashboard);

        sut.IsAvailable(NavSection.Accounts).ShouldBeFalse();
    }

    [Fact]
    public void when_a_section_is_registered_twice_then_it_is_still_available()
    {
        var sut = new FeatureAvailabilityService();
        sut.Register(NavSection.Dashboard);
        sut.Register(NavSection.Dashboard);

        sut.IsAvailable(NavSection.Dashboard).ShouldBeTrue();
    }
}
