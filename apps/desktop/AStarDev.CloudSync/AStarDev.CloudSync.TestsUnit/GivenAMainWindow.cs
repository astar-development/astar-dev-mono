using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Domain.DisplaySettings;
using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Overview;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenAMainWindow
{
    [AvaloniaFact]
    public async Task when_shown_then_the_window_is_visible()
    {
        var window = new MainWindow { DataContext = await CreateViewModelAsync() };

        window.Show();

        window.IsVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_title_is_bound_from_the_view_model()
    {
        var window = new MainWindow { DataContext = await CreateViewModelAsync() };

        window.Show();

        window.Title.ShouldBe("AStar Dev CloudSync");
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_content_is_layout_a_bound_to_the_layout_a_view_model()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        FindSingle<LayoutAView>(window).DataContext.ShouldBeSameAs(viewModel.LayoutA);
    }

    [AvaloniaFact]
    public async Task when_layout_b_is_chosen_then_the_root_view_is_swapped_for_layout_b_without_a_restart()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());
        window.UpdateLayout();

        FindSingle<LayoutBView>(window).DataContext.ShouldBeSameAs(viewModel.LayoutB);
        window.GetVisualDescendants().OfType<LayoutAView>().ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_layout_c_is_chosen_then_the_root_view_is_swapped_for_layout_c_without_a_restart()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateSyncPairsFirst());
        window.UpdateLayout();

        FindSingle<LayoutCView>(window).DataContext.ShouldBeSameAs(viewModel.LayoutC);
    }

    [AvaloniaFact]
    public async Task when_layout_b_is_left_and_shown_again_then_its_selected_account_survives_the_view_being_rebuilt()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());
        window.UpdateLayout();
        var photos = viewModel.LayoutB.Overview.Accounts.Single(account => account.Name == "Photos");
        viewModel.LayoutB.SelectedItem = photos;

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateUnifiedAccounts());
        window.UpdateLayout();
        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());
        window.UpdateLayout();

        viewModel.LayoutB.SelectedItem.ShouldBeSameAs(photos);
    }

    [AvaloniaFact]
    public async Task when_layout_a_is_left_and_shown_again_then_its_selected_account_survives_the_view_being_rebuilt()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        var photos = viewModel.LayoutA.VisibleAccounts.Single(card => card.Account.Name == "Photos");
        photos.SelectCommand.Execute(null);

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());
        window.UpdateLayout();
        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateUnifiedAccounts());
        window.UpdateLayout();

        viewModel.LayoutA.VisibleAccounts.Where(card => card.IsSelected).ShouldHaveSingleItem().ShouldBeSameAs(photos);
        window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-row") && button.Classes.Contains("selected")).ShouldHaveSingleItem().DataContext.ShouldBeSameAs(photos);
    }

    [AvaloniaTheory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public async Task when_any_layout_is_shown_then_its_settings_button_is_bound_to_the_display_settings(string key)
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.FromKey(key));
        window.UpdateLayout();

        FindSingle<DisplaySettingsButton>(window).DataContext.ShouldBeSameAs(viewModel.DisplaySettings);
    }

    [AvaloniaFact]
    public async Task when_the_settings_flyout_opens_then_it_shows_the_display_settings()
    {
        var viewModel = await CreateViewModelAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        var button = FindSingle<DisplaySettingsButton>(window).GetVisualDescendants().OfType<Button>().Single();
        var flyout = button.Flyout.ShouldBeOfType<Flyout>();

        flyout.ShowAt(button);

        flyout.Content.ShouldBeOfType<DisplaySettingsView>().DataContext.ShouldBeSameAs(viewModel.DisplaySettings);
        flyout.Hide();
    }

    [AvaloniaFact]
    public async Task when_the_overview_fails_to_load_then_the_window_shows_the_load_error()
    {
        var overview = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var window = new MainWindow { DataContext = TestData.CreateMainWindowViewModel(overview, TestData.CreateDisplaySettings()) };
        window.Show();

        await overview.LoadAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();

        window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ShouldContain("Could not load your accounts and sync pairs. The details have been logged.");
    }

    [AvaloniaFact]
    public async Task when_the_overview_loads_then_no_load_error_is_shown()
    {
        var window = new MainWindow { DataContext = await CreateViewModelAsync() };

        window.Show();

        window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ShouldNotContain("Could not load your accounts and sync pairs. The details have been logged.");
    }

    [AvaloniaFact]
    public async Task when_the_add_account_dialog_is_open_then_the_window_shows_it_over_the_layout()
    {
        var signIn = new TaskCompletionSource<AccountAddOutcome>();
        var addAccount = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => signIn.Task });
        var window = new MainWindow { DataContext = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings(), addAccount) };
        window.Show();

        var shown = addAccount.ShowAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();

        FindSingle<AddAccountView>(window).IsEffectivelyVisible.ShouldBeTrue();
        window.FindControl<ContentControl>("LayoutHost").ShouldNotBeNull().IsEffectivelyEnabled.ShouldBeFalse();
        signIn.SetResult(AccountAddOutcomeFactory.CreateCancelled());
        await shown;
    }

    [AvaloniaFact]
    public async Task when_the_add_account_dialog_is_closed_then_the_window_does_not_show_it()
    {
        var window = new MainWindow { DataContext = await CreateViewModelAsync() };

        window.Show();

        window.GetVisualDescendants().OfType<AddAccountView>().ShouldAllBe(view => !view.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task when_the_plaintext_token_question_shows_then_dont_add_the_account_cancels_the_attempt()
    {
        var consent = new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), TestData.CreateLocalization());
        var adder = new FakeAccountAdder { Add = async token => { await ((Task)consent.AskAsync(token)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext); return AccountAddOutcomeFactory.CreateCancelled(); } };
        var addAccount = new AddAccountViewModel(adder, consent, TestData.CreateLocalization(), NullLogger<AddAccountViewModel>.Instance);
        var window = new MainWindow { DataContext = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings(), addAccount) };
        window.Show();

        var shown = addAccount.ShowAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();
        var declineButton = FindSingle<AddAccountView>(window).GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, consent.DeclineText));

        declineButton.Command.ShouldBeSameAs(addAccount.CancelCommand);
        addAccount.CancelCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static T FindSingle<T>(Window window) where T : Control => window.GetVisualDescendants().OfType<T>().ShouldHaveSingleItem();

    private static async Task<MainWindowViewModel> CreateViewModelAsync() => TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
}
