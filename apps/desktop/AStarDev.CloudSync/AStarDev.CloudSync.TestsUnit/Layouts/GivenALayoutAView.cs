using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutAView
{
    [AvaloniaFact]
    public async Task when_shown_at_the_default_window_size_then_the_account_cards_scroll()
    {
        var window = await CreateShownWindowAsync();

        var cards = window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards");

        cards.Extent.Height.ShouldBeGreaterThan(cards.Viewport.Height);
        cards.Viewport.Height.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public async Task when_shown_then_a_card_is_rendered_for_every_visible_account()
    {
        var window = await CreateShownWindowAsync();

        window.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("account-card")).ShouldBe(4);
    }

    [AvaloniaFact]
    public async Task when_there_are_more_accounts_than_fit_then_the_account_list_scrolls_in_a_bounded_viewport()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([.. Enumerable.Range(0, 30).Select(_ => TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()))], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var accountList = window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountList");
        accountList.Extent.Height.ShouldBeGreaterThan(accountList.Viewport.Height);
        accountList.Viewport.Height.ShouldBeGreaterThan(0);
        accountList.Viewport.Height.ShouldBeLessThan(window.Height);
    }

    [AvaloniaFact]
    public async Task when_shown_then_only_the_selected_account_row_is_highlighted()
    {
        var window = await CreateShownWindowAsync();

        var rows = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-row")).ToList();

        rows.Count.ShouldBe(4);
        rows.Where(row => row.Classes.Contains("selected")).ShouldHaveSingleItem().DataContext.ShouldBeOfType<LayoutAAccountCardViewModel>().Account.Name.ShouldBe("Work");
    }

    [AvaloniaFact]
    public async Task when_an_account_row_is_clicked_then_it_becomes_the_highlighted_row()
    {
        var window = await CreateShownWindowAsync();
        var photosRow = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("account-row") && button.DataContext is LayoutAAccountCardViewModel { Account.Name: "Photos" });

        photosRow.Command.ShouldNotBeNull().Execute(photosRow.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-row") && button.Classes.Contains("selected")).ShouldHaveSingleItem().ShouldBeSameAs(photosRow);
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_card_action_and_the_add_account_button_are_bound_to_their_commands()
    {
        var window = await CreateShownWindowAsync();
        var layout = window.Content.ShouldBeOfType<LayoutAView>().DataContext.ShouldBeOfType<LayoutAViewModel>();
        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();

        var cardActions = buttons.Where(button => button.Classes.Contains("card-action")).ToList();

        cardActions.ShouldNotBeEmpty();
        cardActions.ShouldAllBe(button => button.Command == ((LayoutAAccountCardViewModel)button.DataContext!).ActionCommand);
        buttons.Single(button => button.Classes.Contains("add-account")).Command.ShouldBeSameAs(layout.AddAccountCommand);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_rail_button_is_bound_to_the_disabled_navigation_command()
    {
        var window = await CreateShownWindowAsync();
        var layout = window.Content.ShouldBeOfType<LayoutAView>().DataContext.ShouldBeOfType<LayoutAViewModel>();

        var railButtons = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("rail")).ToList();

        railButtons.Count.ShouldBe(5);
        railButtons.ShouldAllBe(button => button.Command == layout.NavigateCommand && !button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task when_the_rail_buttons_are_disabled_then_the_active_one_keeps_its_highlight_and_the_rest_stay_clear()
    {
        var window = await CreateShownWindowAsync();
        window.TryFindResource("CloudSyncAccentSoftBrush", window.ActualThemeVariant, out var accentSoft).ShouldBeTrue();

        var railButtons = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("rail")).ToList();
        var backgrounds = railButtons.Select(button => button.GetVisualDescendants().OfType<ContentPresenter>().First(presenter => presenter.Name == "PART_ContentPresenter").Background).ToList();

        backgrounds[0].ShouldBeSameAs(accentSoft);
        backgrounds.Skip(1).ShouldAllBe(background => background is ISolidColorBrush && ((ISolidColorBrush)background).Color.A == 0);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
