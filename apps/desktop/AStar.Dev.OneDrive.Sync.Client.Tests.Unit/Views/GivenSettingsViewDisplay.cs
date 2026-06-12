using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenSettingsViewDisplay
{
    private static SettingsViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Current.Returns(new AppSettings());

        var themeService = Substitute.For<IThemeService>();
        var scheduler = Substitute.For<ISyncScheduler>();
        var repository = Substitute.For<IAccountRepository>();
        var folderPickerService = Substitute.For<IFolderPickerService>();

        return new SettingsViewModel(settingsService, themeService, scheduler, repository, localization, folderPickerService);
    }

    private static SettingsView CreateViewWithViewModel(SettingsViewModel viewModel)
    {
        var view = new SettingsView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));

        return view;
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_scroll_viewer_with_auto_vertical_scrolling_is_present()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var scrollViewer = sut.GetLogicalDescendants().OfType<ScrollViewer>().FirstOrDefault(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto && sv.MinHeight == 0);
        scrollViewer.ShouldNotBeNull("settings ScrollViewer must declare VerticalScrollBarVisibility=Auto and MinHeight=0");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_theme_items_control_is_bound_to_theme_options()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var themeItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.ThemeOptions));
        themeItemsControl.ShouldNotBeNull("ItemsControl for theme buttons must be bound to ThemeOptions");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_language_items_control_is_bound_to_language_options()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var languageItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.LanguageOptions));
        languageItemsControl.ShouldNotBeNull("ItemsControl for language buttons must be bound to LanguageOptions");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_policy_items_control_is_bound_to_policy_options()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var policyItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.PolicyOptions));
        policyItemsControl.ShouldNotBeNull("ItemsControl for conflict policy buttons must be bound to PolicyOptions");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_interval_items_control_is_bound_to_interval_options()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var intervalItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.IntervalOptions));
        intervalItemsControl.ShouldNotBeNull("ItemsControl for sync interval buttons must be bound to IntervalOptions");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_worker_count_items_control_is_bound_to_worker_count_options()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var workerItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.WorkerCountOptions));
        workerItemsControl.ShouldNotBeNull("ItemsControl for worker count buttons must be bound to WorkerCountOptions");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_account_settings_items_control_is_bound_to_account_settings()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var accountItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.AccountSettings));
        accountItemsControl.ShouldNotBeNull("ItemsControl for per-account settings must be bound to AccountSettings");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_language_description_echoes_localization_key()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var descriptionBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "Settings.Language.Description");
        descriptionBlock.ShouldNotBeNull("Language description TextBlock must display the echoed localization key");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_at_least_six_items_controls_are_present()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var itemsControls = sut.GetLogicalDescendants().OfType<ItemsControl>().ToList();
        itemsControls.Count.ShouldBeGreaterThanOrEqualTo(6, "settings view should contain ItemsControls for: theme, language, policy, interval, worker-count, account-settings");
    }

    [AvaloniaFact]
    public void when_settings_view_is_rendered_then_account_settings_collection_starts_empty()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var accountItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().First(ic => ReferenceEquals(ic.ItemsSource, viewModel.AccountSettings));
        viewModel.AccountSettings.Count.ShouldBe(0, "AccountSettings should be empty before any accounts are loaded");
        accountItemsControl.ItemsSource.ShouldBe(viewModel.AccountSettings);
    }
}
