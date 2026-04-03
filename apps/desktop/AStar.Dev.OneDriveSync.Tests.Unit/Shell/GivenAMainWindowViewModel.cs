using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using NSubstitute;
using ReactiveUI;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Shell;

public sealed class GivenAMainWindowViewModel
{
    private readonly INavigationService          _navigationService   = Substitute.For<INavigationService>();
    private readonly IFeatureAvailabilityService _featureAvailability = Substitute.For<IFeatureAvailabilityService>();
    private readonly ILocalisationService        _localisationService = Substitute.For<ILocalisationService>();
    private readonly IShellNavigator             _shellNavigator      = Substitute.For<IShellNavigator>();

    public GivenAMainWindowViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _ = _localisationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        _ = _localisationService.GetString("Nav_ComingSoonSuffix").Returns("— Coming soon");
    }

    [Theory]
    [InlineData(NavSection.Dashboard)]
    [InlineData(NavSection.Accounts)]
    [InlineData(NavSection.Activity)]
    [InlineData(NavSection.Conflicts)]
    [InlineData(NavSection.LogViewer)]
    [InlineData(NavSection.Settings)]
    [InlineData(NavSection.Help)]
    [InlineData(NavSection.About)]
    public void when_navigate_is_invoked_then_the_active_view_is_updated(NavSection section)
    {
        var expectedView = new TestViewModel();
        _ = _featureAvailability.IsAvailable(section).Returns(true);
        _ = _navigationService.ResolveView(section).Returns(expectedView);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(section).Subscribe(new Subject<System.Reactive.Unit>());

        sut.ActiveView.ShouldBe(expectedView);
    }

    [Theory]
    [InlineData(NavSection.Dashboard)]
    [InlineData(NavSection.Accounts)]
    [InlineData(NavSection.Activity)]
    [InlineData(NavSection.Conflicts)]
    [InlineData(NavSection.LogViewer)]
    [InlineData(NavSection.Settings)]
    [InlineData(NavSection.Help)]
    [InlineData(NavSection.About)]
    public void when_navigate_is_invoked_then_the_selected_section_is_updated(NavSection section)
    {
        var expectedView = new TestViewModel();
        _ = _featureAvailability.IsAvailable(section).Returns(true);
        _ = _navigationService.ResolveView(section).Returns(expectedView);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(section).Subscribe(new Subject<System.Reactive.Unit>());

        sut.SelectedSection.ShouldBe(section);
    }

    [Theory]
    [InlineData(NavSection.Dashboard)]
    [InlineData(NavSection.Accounts)]
    [InlineData(NavSection.Activity)]
    [InlineData(NavSection.Conflicts)]
    [InlineData(NavSection.LogViewer)]
    [InlineData(NavSection.Settings)]
    [InlineData(NavSection.Help)]
    [InlineData(NavSection.About)]
    public void when_navigate_is_invoked_then_the_target_nav_item_becomes_active(NavSection section)
    {
        _ = _featureAvailability.IsAvailable(section).Returns(true);
        _ = _navigationService.ResolveView(section).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(section).Subscribe(new Subject<System.Reactive.Unit>());

        var targetItem = sut.NavItems.Single(i => i.Section == section);

        targetItem.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(NavSection.Dashboard)]
    [InlineData(NavSection.Accounts)]
    [InlineData(NavSection.Activity)]
    [InlineData(NavSection.Conflicts)]
    [InlineData(NavSection.LogViewer)]
    [InlineData(NavSection.Settings)]
    [InlineData(NavSection.Help)]
    [InlineData(NavSection.About)]
    public void when_navigate_is_invoked_then_all_other_nav_items_become_inactive(NavSection section)
    {
        _ = _featureAvailability.IsAvailable(section).Returns(true);
        _ = _navigationService.ResolveView(section).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(section).Subscribe(new Subject<System.Reactive.Unit>());

        var inactiveItems = sut.NavItems.Where(i => i.Section != section);

        inactiveItems.ShouldAllBe(i => !i.IsActive);
    }

    [Fact]
    public void when_navigate_is_invoked_for_a_disabled_feature_then_the_active_view_does_not_change()
    {
        var dashboardView = new TestViewModel();
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _featureAvailability.IsAvailable(NavSection.Accounts).Returns(false);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(dashboardView);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(NavSection.Dashboard).Subscribe(new Subject<System.Reactive.Unit>());
        var viewBeforeAttempt = sut.ActiveView;

        _ = sut.NavigateCommand.Execute(NavSection.Accounts).Subscribe(new Subject<System.Reactive.Unit>());

        sut.ActiveView.ShouldBe(viewBeforeAttempt);
    }

    [Fact]
    public void when_navigate_is_invoked_for_a_disabled_feature_then_the_selected_section_does_not_change()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _featureAvailability.IsAvailable(NavSection.Accounts).Returns(false);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        _ = sut.NavigateCommand.Execute(NavSection.Dashboard).Subscribe(new Subject<System.Reactive.Unit>());

        _ = sut.NavigateCommand.Execute(NavSection.Accounts).Subscribe(new Subject<System.Reactive.Unit>());

        sut.SelectedSection.ShouldBe(NavSection.Dashboard);
    }

    [Fact]
    public void when_the_view_model_is_created_then_is_loading_is_true()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void when_the_view_model_is_created_then_is_nav_enabled_is_false()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.IsNavEnabled.ShouldBeFalse();
    }

    [Fact]
    public void when_startup_completes_then_is_loading_becomes_false()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.CompleteStartup();

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void when_startup_completes_then_is_nav_enabled_becomes_true()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.CompleteStartup();

        sut.IsNavEnabled.ShouldBeTrue();
    }

    [Fact]
    public void when_startup_completes_then_the_dashboard_view_is_active()
    {
        var dashboardView = new TestViewModel();
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(dashboardView);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.CompleteStartup();

        sut.ActiveView.ShouldBe(dashboardView);
    }

    [Fact]
    public void when_startup_completes_then_the_dashboard_nav_item_is_active()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);
        _ = _navigationService.ResolveView(NavSection.Dashboard).Returns(new TestViewModel());

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.CompleteStartup();

        sut.NavItems[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public void when_a_startup_error_is_set_then_has_startup_error_is_true()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.SetStartupError("Database is corrupt.");

        sut.HasStartupError.ShouldBeTrue();
    }

    [Fact]
    public void when_a_startup_error_is_set_then_the_error_message_is_stored()
    {
        const string errorMessage = "Database is corrupt.";
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        sut.SetStartupError(errorMessage);

        sut.StartupErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void when_the_view_model_is_created_then_eight_nav_items_are_present()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.NavItems.Count.ShouldBe(8);
    }

    [Fact]
    public void when_the_view_model_is_created_then_nav_items_appear_in_spec_order()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.NavItems[0].Section.ShouldBe(NavSection.Dashboard);
        sut.NavItems[1].Section.ShouldBe(NavSection.Accounts);
        sut.NavItems[2].Section.ShouldBe(NavSection.Activity);
        sut.NavItems[3].Section.ShouldBe(NavSection.Conflicts);
        sut.NavItems[4].Section.ShouldBe(NavSection.LogViewer);
        sut.NavItems[5].Section.ShouldBe(NavSection.Settings);
        sut.NavItems[6].Section.ShouldBe(NavSection.Help);
        sut.NavItems[7].Section.ShouldBe(NavSection.About);
    }

    [Fact]
    public void when_the_view_model_is_created_then_top_nav_items_are_the_first_five_sections()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.TopNavItems.Count.ShouldBe(5);
        sut.TopNavItems[0].Section.ShouldBe(NavSection.Dashboard);
        sut.TopNavItems[4].Section.ShouldBe(NavSection.LogViewer);
    }

    [Fact]
    public void when_the_view_model_is_created_then_bottom_nav_items_are_the_last_three_sections()
    {
        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.BottomNavItems.Count.ShouldBe(3);
        sut.BottomNavItems[0].Section.ShouldBe(NavSection.Settings);
        sut.BottomNavItems[2].Section.ShouldBe(NavSection.About);
    }

    [Fact]
    public void when_a_feature_is_available_then_its_nav_item_is_enabled()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Dashboard).Returns(true);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);

        sut.NavItems[0].IsFeatureEnabled.ShouldBeTrue();
    }

    [Fact]
    public void when_a_feature_is_unavailable_then_its_nav_item_is_disabled_with_a_tooltip()
    {
        _ = _featureAvailability.IsAvailable(NavSection.Accounts).Returns(false);

        var sut = new MainWindowViewModel(_navigationService, _featureAvailability, _localisationService, _shellNavigator);
        var accountsItem = sut.NavItems.Single(i => i.Section == NavSection.Accounts);

        accountsItem.IsFeatureEnabled.ShouldBeFalse();
        accountsItem.Tooltip.ShouldContain("Coming soon");
    }

    private sealed class TestViewModel : ViewModelBase { }
}
