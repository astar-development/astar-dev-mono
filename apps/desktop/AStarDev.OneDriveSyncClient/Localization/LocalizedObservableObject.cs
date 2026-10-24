using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.Localization;

/// <summary>
/// Base for view models that display localised text. Subscribes weakly to <see cref="ILocalizationService.CultureChanged"/>
/// so short-lived instances are never kept alive, and raises <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
/// for every property when the culture changes.
/// </summary>
public abstract class LocalizedObservableObject : ObservableObject
{
    /// <summary>The localisation service this view model was created with.</summary>
    protected ILocalizationService Localization { get; }

    /// <summary>Subscribes to culture changes on <paramref name="localization"/>.</summary>
    protected LocalizedObservableObject(ILocalizationService localization)
    {
        Localization = localization;
        var weakSelf = new WeakReference<LocalizedObservableObject>(this);
        EventHandler<CultureInfo>? handler = null;
        handler = (_, _) =>
        {
            if (weakSelf.TryGetTarget(out var target))
                target.OnCultureChanged();
            else
                localization.CultureChanged -= handler;
        };
        localization.CultureChanged += handler;
    }

    /// <summary>Called after the culture changes. Override to recompute stored text, then call the base implementation.</summary>
    protected virtual void OnCultureChanged() => OnPropertyChanged(string.Empty);
}
