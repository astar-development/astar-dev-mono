using AStarDev.CloudSync.Core.DisplaySettings;
using AStarDev.CloudSync.Domain.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The layout and theme choices: applies a change immediately and saves it for the next start.</summary>
public sealed class DisplaySettingsViewModel : LocalizedObservableObject
{
    private readonly IDisplayPreferencesStore store;
    private readonly IThemeApplier themeApplier;
    private DisplayPreferences preferences;

    /// <summary>Loads the saved preferences and applies the saved theme.</summary>
    /// <param name="store">Loads and saves the preferences.</param>
    /// <param name="themeApplier">Applies the theme to the running application.</param>
    /// <param name="plaintextCacheConsent">The setting that withdraws a remembered consent to the plaintext token file.</param>
    /// <param name="localization">Supplies the text.</param>
    public DisplaySettingsViewModel(IDisplayPreferencesStore store, IThemeApplier themeApplier, PlaintextCacheConsentSettingsViewModel plaintextCacheConsent, ILocalizationService localization) : base(localization)
    {
        PlaintextCacheConsent = plaintextCacheConsent;
        this.store = store;
        this.themeApplier = themeApplier;
        preferences = store.Load();
        LayoutOptions =
        [
            new DisplayOptionViewModel("Settings.Layout.UnifiedAccounts", () => SelectLayout(MainLayoutFactory.CreateUnifiedAccounts()), localization),
            new DisplayOptionViewModel("Settings.Layout.GroupedByProvider", () => SelectLayout(MainLayoutFactory.CreateGroupedByProvider()), localization),
            new DisplayOptionViewModel("Settings.Layout.SyncPairsFirst", () => SelectLayout(MainLayoutFactory.CreateSyncPairsFirst()), localization)
        ];
        ThemeOptions =
        [
            new DisplayOptionViewModel("Settings.Theme.Dark", () => SelectTheme(AppThemeFactory.CreateDark()), localization),
            new DisplayOptionViewModel("Settings.Theme.Light", () => SelectTheme(AppThemeFactory.CreateLight()), localization)
        ];
        themeApplier.Apply(preferences.Theme);
        RefreshSelection();
    }

    /// <summary>The current main window layout.</summary>
    public MainLayout CurrentLayout => preferences.Layout;

    /// <summary>The current colour theme.</summary>
    public AppTheme CurrentTheme => preferences.Theme;

    /// <summary>Layouts A, B and C, in that order.</summary>
    public IReadOnlyList<DisplayOptionViewModel> LayoutOptions { get; }

    /// <summary>Dark, then light.</summary>
    public IReadOnlyList<DisplayOptionViewModel> ThemeOptions { get; }

    /// <summary>The setting that withdraws a remembered consent to the plaintext token file.</summary>
    public PlaintextCacheConsentSettingsViewModel PlaintextCacheConsent { get; }

    /// <summary>The settings panel title and settings button name.</summary>
    public string Title => Localization.GetLocal("Settings.Title");

    /// <summary>The layout group heading.</summary>
    public string LayoutText => Localization.GetLocal("Settings.Layout");

    /// <summary>The theme group heading.</summary>
    public string ThemeText => Localization.GetLocal("Settings.Theme");

    /// <summary>Switches the main window layout and saves the choice.</summary>
    /// <param name="layout">The layout to show.</param>
    public void SelectLayout(MainLayout layout)
    {
        if (layout == preferences.Layout)
            return;

        preferences = DisplayPreferencesFactory.Create(layout, preferences.Theme);
        store.Save(preferences);
        RefreshSelection();
        OnPropertyChanged(nameof(CurrentLayout));
    }

    /// <summary>Applies the theme and saves the choice.</summary>
    /// <param name="theme">The theme to apply.</param>
    public void SelectTheme(AppTheme theme)
    {
        if (theme == preferences.Theme)
            return;

        preferences = DisplayPreferencesFactory.Create(preferences.Layout, theme);
        themeApplier.Apply(theme);
        store.Save(preferences);
        RefreshSelection();
        OnPropertyChanged(nameof(CurrentTheme));
    }

    private void RefreshSelection()
    {
        LayoutOptions[0].IsSelected = preferences.Layout is MainLayout.UnifiedAccounts;
        LayoutOptions[1].IsSelected = preferences.Layout is MainLayout.GroupedByProvider;
        LayoutOptions[2].IsSelected = preferences.Layout is MainLayout.SyncPairsFirst;
        ThemeOptions[0].IsSelected = preferences.Theme is AppTheme.Dark;
        ThemeOptions[1].IsSelected = preferences.Theme is AppTheme.Light;
    }
}
