using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>View model for the CloudSync main window.</summary>
/// <param name="localization">The localisation service supplying the window's text.</param>
public sealed class MainWindowViewModel(ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The window title.</summary>
    public string Title => Localization.GetLocal("App.Title");
}
