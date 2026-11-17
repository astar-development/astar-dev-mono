using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The reclassify button of the classification page: starts a run, shows the files done, lets the user cancel and reloads the term list afterwards. A run it did not start, such as the startup reclassify, is shown read-only: the button is disabled and there is no cancel, because the page does not own that run's token (#1906). The progress callback is created when the command starts, on the UI thread, so reports are marshalled back to it.</summary>
public sealed class ReclassifyViewModel : LocalizedObservableObject, IDisposable
{
    private readonly IClassificationReclassifier reclassifier;
    private readonly IReclassifyRunRecordedSource runRecorded;
    private readonly IReclassifyProgressSource progressSource;
    private readonly IUiDispatcher dispatcher;
    private readonly IReclassifyFreshnessCheck freshnessCheck;
    private readonly ClassificationTermListViewModel terms;
    private bool disposed;
    private ReclassifyState state = ReclassifyStateFactory.CreateIdle();
    private ReclassifyFreshness freshness = ReclassifyFreshnessFactory.CreateUpToDate();

    /// <summary>Creates the view model in the idle state.</summary>
    /// <param name="reclassifier">Runs the reclassify; a singleton, so a second run is refused.</param>
    /// <param name="runRecorded">Raises an event when a run was recorded, so the recommendation refreshes after a run this view model did not start.</param>
    /// <param name="progressSource">Shows the progress of a run this view model did not start.</param>
    /// <param name="dispatcher">Moves the refresh onto the UI thread.</param>
    /// <param name="freshnessCheck">Tells whether a rerun is worth recommending.</param>
    /// <param name="terms">The term list reloaded after a run.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ReclassifyViewModel(IClassificationReclassifier reclassifier, IReclassifyRunRecordedSource runRecorded, IReclassifyProgressSource progressSource, IUiDispatcher dispatcher, IReclassifyFreshnessCheck freshnessCheck, ClassificationTermListViewModel terms, ILocalizationService localization) : base(localization)
    {
        this.reclassifier = reclassifier;
        this.runRecorded = runRecorded;
        this.progressSource = progressSource;
        this.dispatcher = dispatcher;
        this.freshnessCheck = freshnessCheck;
        this.terms = terms;
        ReclassifyCommand = new AsyncRelayCommand(RunAsync, () => state is not ReclassifyState.Observing);
        CancelCommand = new RelayCommand(ReclassifyCommand.Cancel, () => IsRunning);
        CheckCommand = new AsyncRelayCommand(CheckAsync);
        runRecorded.RunRecorded += OnRunRecorded;
        progressSource.Changed += OnProgressChanged;
        ShowObserved();
    }

    /// <summary>Where the button is.</summary>
    public ReclassifyState State => state;

    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning => state is ReclassifyState.Running;

    /// <summary>Whether a run is in progress, started here or elsewhere; drives the progress bar.</summary>
    public bool IsBusy => state is ReclassifyState.Running or ReclassifyState.Observing;

    /// <summary>Whether there is a status line to show.</summary>
    public bool HasStatus => state is not ReclassifyState.Idle;

    /// <summary>The reclassify button text.</summary>
    public string ButtonText => Localization.GetLocal("Classification.Reclassify.Button");

    /// <summary>The cancel button text.</summary>
    public string CancelText => Localization.GetLocal("Classification.Reclassify.Cancel");

    /// <summary>The progress or outcome line; empty while idle.</summary>
    public string StatusText => state switch
    {
        ReclassifyState.Running { FilesSkipped: > 0 } running => Localization.GetLocal("Classification.Reclassify.RunningWithSkipped", Format(running.FilesDone), Format(running.FilesSkipped)),
        ReclassifyState.Running running => Localization.GetLocal("Classification.Reclassify.Running", Format(running.FilesDone)),
        ReclassifyState.Observing { FilesSkipped: > 0 } observing => Localization.GetLocal("Classification.Reclassify.RunningWithSkipped", Format(observing.FilesDone), Format(observing.FilesSkipped)),
        ReclassifyState.Observing observing => Localization.GetLocal("Classification.Reclassify.Running", Format(observing.FilesDone)),
        ReclassifyState.Completed { Counts.SkippedForAccount: > 0 } completed => Localization.GetLocal("Classification.Reclassify.CompletedWithSkipped", Format(completed.Counts.FilesWritten), Format(completed.Counts.Batches), Format(completed.Counts.SkippedForAccount)),
        ReclassifyState.Completed completed => Localization.GetLocal("Classification.Reclassify.Completed", Format(completed.Counts.FilesWritten), Format(completed.Counts.Batches)),
        ReclassifyState.Cancelled cancelled => Localization.GetLocal("Classification.Reclassify.Cancelled", Format(cancelled.Counts.FilesWritten)),
        ReclassifyState.Failed failed => Localization.GetLocal("Classification.Reclassify.Failed", Format(failed.Counts.FilesWritten)),
        ReclassifyState.AlreadyRunning => Localization.GetLocal("Classification.Reclassify.AlreadyRunning"),
        _ => string.Empty
    };

    /// <summary>Whether the last completed run used another engine or seed version, or no run has completed yet. False until <see cref="CheckCommand"/> runs, and when the check fails.</summary>
    public bool IsRerunRecommended => freshness is ReclassifyFreshness.Stale;

    /// <summary>The line that recommends a rerun.</summary>
    public string RerunRecommendedText => Localization.GetLocal("Classification.Reclassify.RerunRecommended");

    /// <summary>Checks whether a rerun is worth recommending; run it each time the page is shown.</summary>
    public IAsyncRelayCommand CheckCommand { get; }

    /// <summary>Starts a run.</summary>
    public IAsyncRelayCommand ReclassifyCommand { get; }

    /// <summary>Cancels the run in progress.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Stops listening for recorded runs and cancels a run in progress, so the app shutting down does not leave the reclassify working.</summary>
    public void Dispose()
    {
        disposed = true;
        runRecorded.RunRecorded -= OnRunRecorded;
        progressSource.Changed -= OnProgressChanged;
        ReclassifyCommand.Cancel();
    }

    private void OnRunRecorded(object? sender, EventArgs e) => _ = dispatcher.InvokeAsync(() => _ = CheckCommand.ExecuteAsync(null));

    private void OnProgressChanged(object? sender, EventArgs e) => _ = dispatcher.InvokeAsync(() => _ = ObserveAsync());

    private async Task ObserveAsync()
    {
        var observe = Try.RunAsync(ShowObservedAsync, CancellationToken.None).MatchAsync(done => done, _ => Unit.Instance);
        await ((Task)observe).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task<Unit> ShowObservedAsync()
    {
        if (disposed)
            return Unit.Instance;

        var wasObserving = state is ReclassifyState.Observing;
        ShowObserved();
        if (wasObserving && state is ReclassifyState.Idle)
        {
            await terms.LoadCommand.ExecuteAsync(null).ConfigureAwait(true);
            await CheckCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        return Unit.Instance;
    }

    private void ShowObserved()
    {
        if (state is ReclassifyState.Running)
            return;

        progressSource.Current.Match(Observe, EndObserving);
    }

    private Unit Observe(ReclassifyProgress report)
    {
        SetState(ReclassifyStateFactory.CreateObserving(report.FilesDone, report.FilesSkipped));

        return Unit.Instance;
    }

    private Unit EndObserving()
    {
        if (state is ReclassifyState.Observing)
            SetState(ReclassifyStateFactory.CreateIdle());

        return Unit.Instance;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        SetState(ReclassifyStateFactory.CreateRunning(0));
        var progress = new Progress<ReclassifyProgress>(report => ShowProgress(report));
        var outcome = await reclassifier.StartAsync(progress, cancellationToken).ConfigureAwait(true);
        SetState(StateOf(outcome));
        if (outcome is ReclassifyOutcome.AlreadyRunning)
            return;

        await terms.LoadCommand.ExecuteAsync(null).ConfigureAwait(true);
        await CheckCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        freshness = await freshnessCheck.CheckAsync(cancellationToken).MatchAsync(current => current, _ => ReclassifyFreshnessFactory.CreateUpToDate()).ConfigureAwait(true);
        OnPropertyChanged(nameof(IsRerunRecommended));
    }

    private void ShowProgress(ReclassifyProgress report)
    {
        if (state is ReclassifyState.Running)
            SetState(ReclassifyStateFactory.CreateRunning(report.FilesDone, report.FilesSkipped));
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
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(StatusText));
        CancelCommand.NotifyCanExecuteChanged();
        ReclassifyCommand.NotifyCanExecuteChanged();
    }
}
