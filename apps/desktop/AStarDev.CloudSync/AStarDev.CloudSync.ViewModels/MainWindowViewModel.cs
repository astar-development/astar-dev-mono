using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>View model for the CloudSync main window.</summary>
/// <param name="localization">The localisation service supplying the window's text.</param>
/// <param name="layoutA">The default main window layout.</param>
public sealed class MainWindowViewModel(ILocalizationService localization, LayoutAViewModel layoutA) : LocalizedObservableObject(localization)
{
    /// <summary>The window title.</summary>
    public string Title => Localization.GetLocal("App.Title");

    /// <summary>Layout A, the default window content.</summary>
    public LayoutAViewModel LayoutA => layoutA;
}
