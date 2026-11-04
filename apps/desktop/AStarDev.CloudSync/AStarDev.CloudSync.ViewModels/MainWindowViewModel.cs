using System.ComponentModel;
using AStarDev.CloudSync.Core;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>View model for the CloudSync main window: shows the layout chosen in the display settings.</summary>
public sealed class MainWindowViewModel : LocalizedObservableObject
{
    /// <summary>Creates the view model and follows layout changes in the display settings.</summary>
    /// <param name="localization">The localisation service supplying the window's text.</param>
    /// <param name="displaySettings">The layout and theme choices.</param>
    /// <param name="layoutA">Layout A, the unified account list.</param>
    /// <param name="layoutB">Layout B, grouped by provider.</param>
    /// <param name="layoutC">Layout C, sync pairs first.</param>
    public MainWindowViewModel(ILocalizationService localization, DisplaySettingsViewModel displaySettings, LayoutAViewModel layoutA, LayoutBViewModel layoutB, LayoutCViewModel layoutC) : base(localization)
    {
        DisplaySettings = displaySettings;
        LayoutA = layoutA;
        LayoutB = layoutB;
        LayoutC = layoutC;
        displaySettings.PropertyChanged += OnDisplaySettingsChanged;
    }

    /// <summary>The window title.</summary>
    public string Title => Localization.GetLocal("App.Title");

    /// <summary>The layout and theme choices.</summary>
    public DisplaySettingsViewModel DisplaySettings { get; }

    /// <summary>Layout A, the unified account list.</summary>
    public LayoutAViewModel LayoutA { get; }

    /// <summary>Layout B, grouped by provider.</summary>
    public LayoutBViewModel LayoutB { get; }

    /// <summary>Layout C, sync pairs first.</summary>
    public LayoutCViewModel LayoutC { get; }

    /// <summary>The view model of the layout to show. Each layout keeps its own state, so switching back restores it.</summary>
    public object CurrentLayout => DisplaySettings.CurrentLayout switch
    {
        MainLayout.GroupedByProvider => LayoutB,
        MainLayout.SyncPairsFirst => LayoutC,
        _ => LayoutA
    };

    private void OnDisplaySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisplaySettingsViewModel.CurrentLayout))
            OnPropertyChanged(nameof(CurrentLayout));
    }
}
