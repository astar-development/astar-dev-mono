using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using ReactiveUnit = System.Reactive.Unit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAFeatureAvailabilityService
{
    [Fact]
    public void when_registering_before_freeze_then_result_is_ok()
    {
        var sut = new FeatureAvailabilityService();

        var result = sut.Register(NavSection.Dashboard);

        result.ShouldBeOfType<Result<ReactiveUnit, string>.Ok>();
    }

    [Fact]
    public void when_registering_after_freeze_then_result_is_error()
    {
        var sut = new FeatureAvailabilityService();
        sut.Freeze();

        var result = sut.Register(NavSection.Dashboard);

        result.ShouldBeOfType<Result<ReactiveUnit, string>.Error>();
    }

    [Fact]
    public void when_registering_after_freeze_then_error_message_is_descriptive()
    {
        var sut = new FeatureAvailabilityService();
        sut.Freeze();

        var result = sut.Register(NavSection.Dashboard);

        var error = result.ShouldBeOfType<Result<ReactiveUnit, string>.Error>();
        error.Reason.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void when_registering_before_freeze_then_section_is_available()
    {
        var sut = new FeatureAvailabilityService();

        sut.Register(NavSection.Accounts);
        sut.Freeze();

        sut.IsAvailable(NavSection.Accounts).ShouldBeTrue();
    }

    [Fact]
    public void when_section_not_registered_then_is_available_returns_false()
    {
        var sut = new FeatureAvailabilityService();
        sut.Freeze();

        sut.IsAvailable(NavSection.Help).ShouldBeFalse();
    }
}
