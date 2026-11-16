using System.ComponentModel;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The settings audit (#1801): pick an account, accept the warning that it can be slow, then see how many files are in its local folder and in the cloud.</summary>
public sealed class FileAuditViewModel : LocalizedObservableObject
{
    private readonly SyncOverviewViewModel overview;
    private readonly IFileAuditor auditor;
    private Option<AccountId> selected = Option.None<AccountId>();
    private CancellationTokenSource? running;

    /// <summary>Creates the setting and follows the accounts the overview shows.</summary>
    /// <param name="overview">Supplies the accounts.</param>
    /// <param name="auditor">Counts the files.</param>
    /// <param name="localization">Supplies the text.</param>
    public FileAuditViewModel(SyncOverviewViewModel overview, IFileAuditor auditor, ILocalizationService localization) : base(localization)
    {
        this.overview = overview;
        this.auditor = auditor;
        overview.PropertyChanged += OnOverviewChanged;
        Accounts = BuildRows();
    }

    /// <summary>One row for each account.</summary>
    public IReadOnlyList<AuditAccountViewModel> Accounts { get; private set; }

    /// <summary>Whether there is at least one account.</summary>
    public bool HasAccounts => Accounts.Count > 0;

    /// <summary>Where the audit is.</summary>
    public FileAuditStep Step { get; private set; } = FileAuditStepFactory.CreateIdle();

    /// <summary>Whether the Audit button shows.</summary>
    public bool ShowsAuditButton => Step is not (FileAuditStep.Confirming or FileAuditStep.Running);

    /// <summary>Whether the audit can be started: an account is chosen and nothing is running.</summary>
    public bool CanAudit => selected.Match(_ => true, () => false) && ShowsAuditButton;

    /// <summary>Whether the slow warning shows.</summary>
    public bool IsConfirming => Step is FileAuditStep.Confirming;

    /// <summary>Whether the audit is running.</summary>
    public bool IsRunning => Step is FileAuditStep.Running;

    /// <summary>Whether the counts show.</summary>
    public bool IsShowingResult => Step is FileAuditStep.Completed;

    /// <summary>Whether the failure message shows.</summary>
    public bool IsShowingError => Step is FileAuditStep.Failed;

    /// <summary>One item, replaced on every run, so the view builds a new progress bar for it (#1805). A content control would reuse its bar and only swap its data context.</summary>
    public IReadOnlyList<object> RunIndicators { get; private set; } = [new object()];

    /// <summary>The group heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.Audit");

    /// <summary>What the audit counts.</summary>
    public string HelpText => Localization.GetLocal("Settings.Audit.Help");

    /// <summary>Shown when there are no accounts.</summary>
    public string NoAccountsText => Localization.GetLocal("Settings.Audit.NoAccounts");

    /// <summary>The label of the Audit button.</summary>
    public string AuditText => Localization.GetLocal("Settings.Audit.Run");

    /// <summary>The warning that the audit can be slow.</summary>
    public string WarningText => Localization.GetLocal("Settings.Audit.Warning");

    /// <summary>The label of the button that runs the audit.</summary>
    public string ConfirmText => Localization.GetLocal("Settings.Audit.Confirm");

    /// <summary>The label of the button that stops or declines the audit.</summary>
    public string CancelText => Localization.GetLocal("Settings.Audit.Cancel");

    /// <summary>Says which account is being audited.</summary>
    public string RunningText => Localization.GetLocal("Settings.Audit.Running", Accounts.Where(row => row.IsSelected).Select(row => row.Name).FirstOrDefault() ?? string.Empty);

    /// <summary>The local file count, or empty before a result.</summary>
    public string LocalFilesText => Step is FileAuditStep.Completed completed ? Localization.GetLocal("Settings.Audit.LocalFiles", Formatted(completed.Counts.LocalFiles)) : string.Empty;

    /// <summary>The cloud file count, or empty before a result.</summary>
    public string CloudFilesText => Step is FileAuditStep.Completed completed ? Localization.GetLocal("Settings.Audit.CloudFiles", Formatted(completed.Counts.CloudFiles)) : string.Empty;

    /// <summary>The failure message, or empty.</summary>
    public string ErrorText => Step is FileAuditStep.Failed failed ? Localization.GetLocal(ErrorKeyOf(failed.Failure)) : string.Empty;

    /// <summary>Shows the warning.</summary>
    public IRelayCommand AuditCommand => field ??= new RelayCommand(ShowWarning, () => CanAudit);

    /// <summary>Runs the audit.</summary>
    public IAsyncRelayCommand ConfirmCommand => field ??= new AsyncRelayCommand(ConfirmAsync, () => IsConfirming);

    /// <summary>Declines the warning, or stops a running audit.</summary>
    public IRelayCommand CancelCommand => field ??= new RelayCommand(Cancel);

    private static string ErrorKeyOf(FileAuditFailure failure) => failure switch
    {
        FileAuditFailure.RootUnavailable => "Settings.Audit.Failed.Root",
        FileAuditFailure.RulesUnavailable => "Settings.Audit.Failed.Rules",
        _ => "Settings.Audit.Failed.Cloud",
    };

    private string Formatted(long count) => count.ToString("N0", Localization.CurrentCulture);

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncOverviewViewModel.Accounts))
            Refresh();
    }

    private void Refresh()
    {
        var stillThere = selected.Match(current => overview.Accounts.Any(account => account.Id == current), () => false);
        if (!stillThere)
            selected = Option.None<AccountId>();

        Accounts = BuildRows();
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(HasAccounts));
        MoveTo(stillThere || IsRunning ? Step : FileAuditStepFactory.CreateIdle());
    }

    private List<AuditAccountViewModel> BuildRows() =>
        [.. overview.Accounts.Select(account => new AuditAccountViewModel(account.Id, account.Name, selected.Match(current => current == account.Id, () => false), () => Select(account.Id)))];

    private void Select(AccountId accountId)
    {
        if (!ShowsAuditButton)
            return;

        selected = Option.Some(accountId);
        Accounts = BuildRows();
        OnPropertyChanged(nameof(Accounts));
        MoveTo(FileAuditStepFactory.CreateIdle());
    }

    private void ShowWarning()
    {
        if (CanAudit)
            MoveTo(FileAuditStepFactory.CreateConfirming());
    }

    private void Cancel()
    {
        if (IsRunning)
            running?.Cancel();
        else
            MoveTo(FileAuditStepFactory.CreateIdle());
    }

    private Task ConfirmAsync() => IsConfirming ? selected.Match(RunAsync, () => Task.CompletedTask) : Task.CompletedTask;

    private async Task RunAsync(AccountId accountId)
    {
        using var source = new CancellationTokenSource();
        running = source;
        RunIndicators = [new object()];
        OnPropertyChanged(nameof(RunIndicators));
        MoveTo(FileAuditStepFactory.CreateRunning());
        var audit = UiResumingTry.RunAsync(
            () => auditor.AuditAsync(accountId, source.Token),
            result => result.Match(FileAuditStepFactory.CreateCompleted, FileAuditStepFactory.CreateFailed),
            _ => FileAuditStepFactory.CreateFailed(FileAuditFailureFactory.CreateCloudUnavailable(ProviderErrorFactory.CreateUnexpected())),
            source.Token);
        await ((Task)audit).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        running = null;
        MoveTo(audit.IsCompletedSuccessfully ? audit.Result : FileAuditStepFactory.CreateIdle());
    }

    private void MoveTo(FileAuditStep step)
    {
        Step = step;
        OnPropertyChanged(nameof(Step));
        OnPropertyChanged(nameof(ShowsAuditButton));
        OnPropertyChanged(nameof(CanAudit));
        OnPropertyChanged(nameof(IsConfirming));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsShowingResult));
        OnPropertyChanged(nameof(IsShowingError));
        OnPropertyChanged(nameof(RunningText));
        OnPropertyChanged(nameof(LocalFilesText));
        OnPropertyChanged(nameof(CloudFilesText));
        OnPropertyChanged(nameof(ErrorText));
        AuditCommand.NotifyCanExecuteChanged();
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
