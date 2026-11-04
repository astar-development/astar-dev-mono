using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenADisplaySettingsViewModel
{
    [Fact]
    public void when_created_then_the_saved_layout_and_theme_are_current_and_the_saved_theme_is_applied()
    {
        var store = new InMemoryDisplayPreferencesStore(DisplayPreferencesFactory.Create(MainLayoutFactory.CreateGroupedByProvider(), AppThemeFactory.CreateLight()));
        var themeApplier = new RecordingThemeApplier();

        var settings = new DisplaySettingsViewModel(store, themeApplier, TestData.CreateLocalization());

        settings.CurrentLayout.ShouldBeOfType<MainLayout.GroupedByProvider>();
        settings.CurrentTheme.ShouldBeOfType<AppTheme.Light>();
        themeApplier.Applied.ShouldHaveSingleItem().ShouldBeOfType<AppTheme.Light>();
    }

    [Fact]
    public void when_created_then_the_options_are_localised_and_only_the_saved_choices_are_selected()
    {
        var store = new InMemoryDisplayPreferencesStore(DisplayPreferencesFactory.Create(MainLayoutFactory.CreateSyncPairsFirst(), AppThemeFactory.CreateDark()));

        var settings = new DisplaySettingsViewModel(store, new RecordingThemeApplier(), TestData.CreateLocalization());

        settings.LayoutOptions.Select(option => option.Label).ShouldBe(["Unified account list", "Grouped by provider", "Sync pairs first"]);
        settings.LayoutOptions.Select(option => option.IsSelected).ShouldBe([false, false, true]);
        settings.ThemeOptions.Select(option => option.Label).ShouldBe(["Dark", "Light"]);
        settings.ThemeOptions.Select(option => option.IsSelected).ShouldBe([true, false]);
        settings.Title.ShouldBe("Display settings");
    }

    [Fact]
    public void when_a_layout_option_is_chosen_then_the_layout_switches_is_saved_and_the_change_is_raised()
    {
        var store = new InMemoryDisplayPreferencesStore();
        var settings = new DisplaySettingsViewModel(store, new RecordingThemeApplier(), TestData.CreateLocalization());
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        settings.LayoutOptions[1].SelectCommand.Execute(null);

        settings.CurrentLayout.ShouldBeOfType<MainLayout.GroupedByProvider>();
        store.Saved.Layout.ShouldBeOfType<MainLayout.GroupedByProvider>();
        store.Saved.Theme.ShouldBeOfType<AppTheme.Dark>();
        settings.LayoutOptions.Select(option => option.IsSelected).ShouldBe([false, true, false]);
        changed.ShouldContain(nameof(DisplaySettingsViewModel.CurrentLayout));
    }

    [Fact]
    public void when_a_theme_option_is_chosen_then_the_theme_is_applied_immediately_and_saved()
    {
        var store = new InMemoryDisplayPreferencesStore();
        var themeApplier = new RecordingThemeApplier();
        var settings = new DisplaySettingsViewModel(store, themeApplier, TestData.CreateLocalization());

        settings.ThemeOptions[1].SelectCommand.Execute(null);

        settings.CurrentTheme.ShouldBeOfType<AppTheme.Light>();
        themeApplier.Applied[^1].ShouldBeOfType<AppTheme.Light>();
        store.Saved.Theme.ShouldBeOfType<AppTheme.Light>();
        store.Saved.Layout.ShouldBeOfType<MainLayout.UnifiedAccounts>();
        settings.ThemeOptions.Select(option => option.IsSelected).ShouldBe([false, true]);
    }

    [Fact]
    public void when_the_current_layout_is_chosen_again_then_nothing_is_saved_or_raised()
    {
        var store = new InMemoryDisplayPreferencesStore();
        var settings = new DisplaySettingsViewModel(store, new RecordingThemeApplier(), TestData.CreateLocalization());
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        settings.SelectLayout(MainLayoutFactory.CreateUnifiedAccounts());

        store.SaveCount.ShouldBe(0);
        changed.ShouldBeEmpty();
    }
}
