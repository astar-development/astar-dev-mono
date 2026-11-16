using AStarDev.CloudSync.Classification;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The reclassify button of the classification page: starts a run, shows the files done, lets the user cancel and reloads the term list afterwards.</summary>
public sealed class ReclassifyViewModel : LocalizedObservableObject
{
    /// <summary>Creates the view model in the idle state.</summary>
    /// <param name="reclassifier">Runs the reclassify; a singleton, so a second run is refused.</param>
    /// <param name="terms">The term list reloaded after a run.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ReclassifyViewModel(IClassificationReclassifier reclassifier, ClassificationTermListViewModel terms, ILocalizationService localization) : base(localization)
    {
    }

    /// <summary>Where the button is.</summary>
    public ReclassifyState State => throw new NotImplementedException();

    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning => throw new NotImplementedException();

    /// <summary>Whether there is a status line to show.</summary>
    public bool HasStatus => throw new NotImplementedException();

    /// <summary>The reclassify button text.</summary>
    public string ButtonText => throw new NotImplementedException();

    /// <summary>The cancel button text.</summary>
    public string CancelText => throw new NotImplementedException();

    /// <summary>The progress or outcome line; empty while idle.</summary>
    public string StatusText => throw new NotImplementedException();

    /// <summary>Starts a run.</summary>
    public IAsyncRelayCommand ReclassifyCommand => throw new NotImplementedException();

    /// <summary>Cancels the run in progress.</summary>
    public IRelayCommand CancelCommand => throw new NotImplementedException();
}
