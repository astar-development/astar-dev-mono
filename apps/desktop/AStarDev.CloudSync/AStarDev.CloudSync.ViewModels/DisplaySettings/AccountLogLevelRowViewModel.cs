using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>One account's "verbose logging" box (#1411).</summary>
public sealed class AccountLogLevelRowViewModel : LocalizedObservableObject
{
    private readonly AccountId accountId;
    private readonly string accountName;
    private readonly IAccountLogLevelSettings settings;
    private AccountLogLevel level;
    private string errorKey = string.Empty;

    /// <summary>Creates the row.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="accountName">The account's display name.</param>
    /// <param name="level">The level the account has now.</param>
    /// <param name="settings">Saves the level.</param>
    /// <param name="localization">Supplies the text.</param>
    public AccountLogLevelRowViewModel(AccountId accountId, string accountName, AccountLogLevel level, IAccountLogLevelSettings settings, ILocalizationService localization) : base(localization)
    {
        this.accountId = accountId;
        this.accountName = accountName;
        this.level = level;
        this.settings = settings;
    }

    /// <summary>The label of the box, naming the account.</summary>
    public string LabelText => Localization.GetLocal("Settings.Logging.Toggle", accountName);

    /// <summary>Whether the account logs verbosely.</summary>
    public bool IsVerbose => level is AccountLogLevel.Verbose;

    /// <summary>The message shown when the change could not be saved, or empty.</summary>
    public string ErrorText => errorKey.Length == 0 ? string.Empty : Localization.GetLocal(errorKey);

    /// <summary>Switches between standard and verbose, and saves the choice at once.</summary>
    public IAsyncRelayCommand ToggleCommand => field ??= new AsyncRelayCommand(ToggleAsync);

    private async Task ToggleAsync(CancellationToken cancellationToken)
    {
        var next = IsVerbose ? AccountLogLevelFactory.CreateStandard() : AccountLogLevelFactory.CreateVerbose();
        _ = await settings.SetAsync(accountId, next, cancellationToken).MatchAsync(_ => Show(next, string.Empty), _ => Show(level, "Settings.Logging.SaveFailed"));
    }

    private Unit Show(AccountLogLevel shown, string key)
    {
        level = shown;
        errorKey = key;
        OnPropertyChanged(nameof(IsVerbose));
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }
}
