using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The settings audit (#1801): pick an account, accept the warning that it can be slow, then see how many files are in its local folder and in the cloud.</summary>
public sealed class FileAuditViewModel : LocalizedObservableObject
{
    /// <summary>Creates the setting and follows the accounts the overview shows.</summary>
    /// <param name="overview">Supplies the accounts.</param>
    /// <param name="auditor">Counts the files.</param>
    /// <param name="localization">Supplies the text.</param>
    public FileAuditViewModel(SyncOverviewViewModel overview, IFileAuditor auditor, ILocalizationService localization) : base(localization) => _ = (overview, auditor);

    /// <summary>One row for each account.</summary>
    public IReadOnlyList<AuditAccountViewModel> Accounts => throw new NotImplementedException(nameof(Accounts));

    /// <summary>Whether there is at least one account.</summary>
    public bool HasAccounts => throw new NotImplementedException(nameof(HasAccounts));

    /// <summary>Where the audit is.</summary>
    public FileAuditStep Step => throw new NotImplementedException(nameof(Step));

    /// <summary>Whether the Audit button shows.</summary>
    public bool ShowsAuditButton => throw new NotImplementedException(nameof(ShowsAuditButton));

    /// <summary>Whether the audit can be started: an account is chosen and nothing is running.</summary>
    public bool CanAudit => throw new NotImplementedException(nameof(CanAudit));

    /// <summary>Whether the slow warning shows.</summary>
    public bool IsConfirming => throw new NotImplementedException(nameof(IsConfirming));

    /// <summary>Whether the audit is running.</summary>
    public bool IsRunning => throw new NotImplementedException(nameof(IsRunning));

    /// <summary>Whether the counts show.</summary>
    public bool IsShowingResult => throw new NotImplementedException(nameof(IsShowingResult));

    /// <summary>Whether the failure message shows.</summary>
    public bool IsShowingError => throw new NotImplementedException(nameof(IsShowingError));

    /// <summary>The group heading.</summary>
    public string HeadingText => throw new NotImplementedException(nameof(HeadingText));

    /// <summary>What the audit counts.</summary>
    public string HelpText => throw new NotImplementedException(nameof(HelpText));

    /// <summary>Shown when there are no accounts.</summary>
    public string NoAccountsText => throw new NotImplementedException(nameof(NoAccountsText));

    /// <summary>The label of the Audit button.</summary>
    public string AuditText => throw new NotImplementedException(nameof(AuditText));

    /// <summary>The warning that the audit can be slow.</summary>
    public string WarningText => throw new NotImplementedException(nameof(WarningText));

    /// <summary>The label of the button that runs the audit.</summary>
    public string ConfirmText => throw new NotImplementedException(nameof(ConfirmText));

    /// <summary>The label of the button that stops or declines the audit.</summary>
    public string CancelText => throw new NotImplementedException(nameof(CancelText));

    /// <summary>Says which account is being audited.</summary>
    public string RunningText => throw new NotImplementedException(nameof(RunningText));

    /// <summary>The local file count, or empty before a result.</summary>
    public string LocalFilesText => throw new NotImplementedException(nameof(LocalFilesText));

    /// <summary>The cloud file count, or empty before a result.</summary>
    public string CloudFilesText => throw new NotImplementedException(nameof(CloudFilesText));

    /// <summary>The failure message, or empty.</summary>
    public string ErrorText => throw new NotImplementedException(nameof(ErrorText));

    /// <summary>Shows the warning.</summary>
    public IRelayCommand AuditCommand => throw new NotImplementedException(nameof(AuditCommand));

    /// <summary>Runs the audit.</summary>
    public IAsyncRelayCommand ConfirmCommand => throw new NotImplementedException(nameof(ConfirmCommand));

    /// <summary>Declines the warning, or stops a running audit.</summary>
    public IRelayCommand CancelCommand => throw new NotImplementedException(nameof(CancelCommand));
}
