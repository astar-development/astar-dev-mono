using AStarDev.CloudSync.Classification;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The reclassify button of the classification page: starts a run, shows the files done, lets the user cancel and reloads the term list afterwards. The progress callback is created when the command starts, on the UI thread, so reports are marshalled back to it.</summary>
public sealed class ReclassifyViewModel : LocalizedObservableObject, IDisposable
{
    private readonly IClassificationReclassifier reclassifier;
    private readonly ClassificationTermListViewModel terms;
    private ReclassifyState state = ReclassifyStateFactory.CreateIdle();

    /// <summary>Creates the view model in the idle state.</summary>
    /// <param name="reclassifier">Runs the reclassify; a singleton, so a second run is refused.</param>
    /// <param name="terms">The term list reloaded after a run.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ReclassifyViewModel(IClassificationReclassifier reclassifier, ClassificationTermListViewModel terms, ILocalizationService localization) : base(localization)
    {
        this.reclassifier = reclassifier;
        this.terms = terms;
        ReclassifyCommand = new AsyncRelayCommand(RunAsync);
        CancelCommand = new RelayCommand(ReclassifyCommand.Cancel, () => IsRunning);
    }

    /// <summary>Where the button is.</summary>
    public ReclassifyState State => state;

    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning => state is ReclassifyState.Running;

    /// <summary>Whether there is a status line to show.</summary>
    public bool HasStatus => state is not ReclassifyState.Idle;

    /// <summary>The reclassify button text.</summary>
    public string ButtonText => Localization.GetLocal("Classification.Reclassify.Button");

    /// <summary>The cancel button text.</summary>
    public string CancelText => Localization.GetLocal("Classification.Reclassify.Cancel");

    /// <summary>The progress or outcome line; empty while idle.</summary>
    public string StatusText => state switch
    {
        ReclassifyState.Running running => Localization.GetLocal("Classification.Reclassify.Running", Format(running.FilesDone)),
        ReclassifyState.Completed completed => Localization.GetLocal("Classification.Reclassify.Completed", Format(completed.Counts.FilesWritten), Format(completed.Counts.Batches)),
        ReclassifyState.Cancelled cancelled => Localization.GetLocal("Classification.Reclassify.Cancelled", Format(cancelled.Counts.FilesWritten)),
        ReclassifyState.Failed failed => Localization.GetLocal("Classification.Reclassify.Failed", Format(failed.Counts.FilesWritten)),
        ReclassifyState.AlreadyRunning => Localization.GetLocal("Classification.Reclassify.AlreadyRunning"),
        _ => string.Empty
    };

    /// <summary>Starts a run.</summary>
    public IAsyncRelayCommand ReclassifyCommand { get; }

    /// <summary>Cancels the run in progress.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Cancels a run in progress, so the app shutting down does not leave the reclassify working.</summary>
    public void Dispose() => throw new NotImplementedException();

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        SetState(ReclassifyStateFactory.CreateRunning(0));
        var progress = new Progress<ReclassifyProgress>(report => ShowProgress(report));
        var outcome = await reclassifier.StartAsync(progress, cancellationToken).ConfigureAwait(true);
        SetState(StateOf(outcome));
        if (outcome is not ReclassifyOutcome.AlreadyRunning)
            await terms.LoadCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    private void ShowProgress(ReclassifyProgress report)
    {
        if (state is ReclassifyState.Running)
            SetState(ReclassifyStateFactory.CreateRunning(report.FilesDone));
    }

    private static ReclassifyState StateOf(ReclassifyOutcome outcome) => outcome switch
    {
        ReclassifyOutcome.Completed completed => ReclassifyStateFactory.CreateCompleted(completed.Counts),
        ReclassifyOutcome.Cancelled cancelled => ReclassifyStateFactory.CreateCancelled(cancelled.Counts),
        ReclassifyOutcome.Failed failed => ReclassifyStateFactory.CreateFailed(failed.Counts),
        _ => ReclassifyStateFactory.CreateAlreadyRunning()
    };

    private string Format(int number) => number.ToString("N0", Localization.CurrentCulture);

    private void SetState(ReclassifyState next)
    {
        state = next;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(StatusText));
        CancelCommand.NotifyCanExecuteChanged();
    }
}
