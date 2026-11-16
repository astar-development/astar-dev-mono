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
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization()) } };

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

        var cardActions = buttons.Where(button => button.Classes.Contains("card-action") && !button.Classes.Contains("card-remove") && !button.Classes.Contains("card-sync-folder") && !button.Classes.Contains("card-pause")).ToList();
        var pauseActions = buttons.Where(button => button.Classes.Contains("card-pause")).ToList();
        var removeActions = buttons.Where(button => button.Classes.Contains("card-remove")).ToList();
        var syncFolderActions = buttons.Where(button => button.Classes.Contains("card-sync-folder")).ToList();

        cardActions.ShouldNotBeEmpty();
        cardActions.ShouldAllBe(button => button.Command == ((LayoutAAccountCardViewModel)button.DataContext!).ActionCommand);
        pauseActions.Count.ShouldBe(cardActions.Count);
        pauseActions.ShouldAllBe(button => button.Command == ((LayoutAAccountCardViewModel)button.DataContext!).PauseCommand);
        removeActions.Count.ShouldBe(cardActions.Count);
        removeActions.ShouldAllBe(button => button.Command == ((LayoutAAccountCardViewModel)button.DataContext!).Account.RemoveCommand);
        syncFolderActions.Count.ShouldBe(cardActions.Count);
        syncFolderActions.ShouldAllBe(button => button.Command == ((LayoutAAccountCardViewModel)button.DataContext!).Account.ChangeSyncFolderCommand);
        buttons.Single(button => button.Classes.Contains("add-account")).Command.ShouldBeSameAs(layout.AddAccountCommand);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_rail_button_navigates_to_its_page()
    {
        var window = await CreateShownWindowAsync();
        var layout = window.Content.ShouldBeOfType<LayoutAView>().DataContext.ShouldBeOfType<LayoutAViewModel>();

        var railButtons = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("rail")).ToList();

        railButtons.Count.ShouldBe(6);
        railButtons.Select(button => button.Command).ShouldBe([layout.ShowAccountsCommand, layout.ShowFoldersCommand, layout.ShowActivityCommand, layout.ShowConflictsCommand, layout.ShowClassificationCommand, layout.ShowSearchCommand]);
        railButtons.Select(button => button.IsEffectivelyEnabled).ShouldBe([true, true, true, true, true, true]);
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

    [AvaloniaFact]
    public async Task when_shown_then_the_accounts_rail_button_is_active_and_the_conflicts_one_is_enabled()
    {
        var window = await CreateShownWindowAsync();

        var rail = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("rail")).ToList();

        Avalonia.Automation.AutomationProperties.GetName(rail.Where(button => button.Classes.Contains("active")).ShouldHaveSingleItem()).ShouldBe("Dashboard");
        rail.Single(button => Avalonia.Automation.AutomationProperties.GetName(button) == "Conflicts").IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_the_conflicts_rail_button_is_clicked_then_the_conflict_list_replaces_the_account_cards()
    {
        var window = await CreateShownWindowAsync();
        var conflictsButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Conflicts");

        conflictsButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards").IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Conflicts.ConflictListContent>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
        conflictsButton.Classes.ShouldContain("active");
    }

    [AvaloniaFact]
    public async Task when_the_folders_rail_button_is_clicked_then_the_folder_selection_replaces_the_account_cards()
    {
        var window = await CreateShownWindowAsync();
        var foldersButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Folders");

        foldersButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards").IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.FolderSelection.FolderSelectionContent>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
        foldersButton.Classes.ShouldContain("active");
    }

    [AvaloniaFact]
    public async Task when_the_activity_rail_button_is_clicked_then_the_activity_list_replaces_the_account_cards()
    {
        var window = await CreateShownWindowAsync();
        var activityButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Activity");

        activityButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards").IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Activity.ActivityListContent>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
        activityButton.Classes.ShouldContain("active");
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_account_card_has_a_pause_or_resume_button_and_only_the_paused_account_reads_resume()
    {
        var window = await CreateShownWindowAsync();

        var texts = window.GetVisualDescendants().OfType<Button>().Select(button => button.Content).OfType<string>().Where(text => text is "Pause" or "Resume").ToList();

        texts.Count(text => text == "Pause").ShouldBe(3);
        texts.Count(text => text == "Resume").ShouldBe(1);
    }

    [AvaloniaFact]
    public async Task when_the_search_rail_button_is_clicked_then_the_search_page_replaces_the_account_cards_and_typing_filters_it()
    {
        var window = await CreateShownWindowAsync();
        var searchButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Search");

        searchButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var box = window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "SearchPageBox");
        box.Text = "recipes";
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards").IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Search.SearchResultsContent>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
        window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible && text.Text == "Recipes").ShouldHaveSingleItem();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Search.SearchResultsContent>().Single().GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible && text.Text == "Accounts").ShouldBeEmpty();
        searchButton.Classes.ShouldContain("active");
    }

    [AvaloniaFact]
    public async Task when_a_folder_search_result_is_clicked_then_the_folders_page_replaces_the_search_page()
    {
        var window = await CreateShownWindowAsync();
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Search").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "SearchPageBox").Text = "recipes";
        Dispatcher.UIThread.RunJobs();

        var result = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("search-result") && button.DataContext is AStarDev.CloudSync.ViewModels.SyncPairs.SyncPairViewModel);
        result.Command!.Execute(result.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Search.SearchResultsContent>().Single().IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.FolderSelection.FolderSelectionContent>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_an_account_search_result_is_clicked_then_the_account_cards_replace_the_search_page()
    {
        var window = await CreateShownWindowAsync();
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Search").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "SearchPageBox").Text = "archive@";
        Dispatcher.UIThread.RunJobs();

        var result = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("search-result") && button.DataContext is AStarDev.CloudSync.ViewModels.Accounts.AccountViewModel);
        result.Command!.Execute(result.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards").IsEffectivelyVisible.ShouldBeTrue();
        window.GetVisualDescendants().OfType<AStarDev.CloudSync.Search.SearchResultsContent>().Single().IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public async Task when_the_conflict_list_is_a_page_then_it_has_no_close_button()
    {
        var window = await CreateShownWindowAsync();
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(button) == "Conflicts").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var content = window.GetVisualDescendants().OfType<AStarDev.CloudSync.Conflicts.ConflictListContent>().Single();

        content.GetVisualDescendants().OfType<Button>().Where(button => Equals(button.Content, "Close")).ShouldAllBe(button => !button.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_main_panel_card_name_has_the_full_name_as_its_tooltip_and_is_trimmed()
    {
        var window = await CreateShownWindowAsync();
        var accounts = window.Content.ShouldBeOfType<LayoutAView>().DataContext.ShouldBeOfType<LayoutAViewModel>().Overview.Accounts;

        var cardNames = window.GetVisualDescendants().OfType<TextBlock>().Where(text => ToolTip.GetTip(text) is string tip && text.Text == tip && accounts.Any(account => account.Name == tip)).ToList();

        cardNames.Count.ShouldBe(accounts.Count);
        cardNames.ShouldAllBe(text => text.TextTrimming == TextTrimming.CharacterEllipsis);
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_main_panel_card_email_line_has_its_full_text_as_its_tooltip()
    {
        var window = await CreateShownWindowAsync();
        var accounts = window.Content.ShouldBeOfType<LayoutAView>().DataContext.ShouldBeOfType<LayoutAViewModel>().Overview.Accounts;

        var emailLines = window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.Text is { Length: > 0 } shown && ToolTip.GetTip(text) is string tip && tip == shown && accounts.Any(account => shown.Contains(account.Email, StringComparison.Ordinal))).ToList();

        emailLines.Count.ShouldBe(accounts.Count);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
