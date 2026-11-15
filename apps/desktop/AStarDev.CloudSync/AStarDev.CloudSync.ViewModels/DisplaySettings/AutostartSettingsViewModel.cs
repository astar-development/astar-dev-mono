using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The "Start CloudSync when I log in" choice (#1410). The registration with the operating system is the stored setting, so the box always shows what the system will do. A change is made at once.</summary>
public sealed class AutostartSettingsViewModel : LocalizedObservableObject
{
    private readonly IAutostartRegistration registration;
    private AutostartState state = AutostartStateFactory.CreateDisabled();
    private bool loaded;
    private string errorKey = string.Empty;

    /// <summary>Creates the setting; call <see cref="LoadAsync"/> to show the current state.</summary>
    /// <param name="registration">Reads and changes the registration.</param>
    /// <param name="localization">Supplies the text.</param>
    public AutostartSettingsViewModel(IAutostartRegistration registration, ILocalizationService localization) : base(localization) => this.registration = registration;

    /// <summary>The group heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.Autostart");

    /// <summary>The label of the box.</summary>
    public string ToggleText => Localization.GetLocal("Settings.Autostart.Toggle");

    /// <summary>Whether the app starts at login.</summary>
    public bool IsEnabled => state is AutostartState.Enabled;

    /// <summary>Whether the box can be changed: the state was read and this build can register.</summary>
    public bool CanChange => loaded && state is not AutostartState.NotSupported;

    /// <summary>Why the box cannot be changed, or empty.</summary>
    public string ReasonText => state is AutostartState.NotSupported { Blocker: var blocker }
        ? Localization.GetLocal(BlockerKey(blocker))
        : string.Empty;

    /// <summary>The message shown when the state could not be read or changed, or empty.</summary>
    public string ErrorText => errorKey.Length == 0 ? string.Empty : Localization.GetLocal(errorKey);

    /// <summary>Turns the registration on when it is off and off when it is on.</summary>
    public IAsyncRelayCommand ToggleCommand => field ??= new AsyncRelayCommand(ToggleAsync, () => CanChange);

    /// <summary>Reads the registration and shows it.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task LoadAsync(CancellationToken cancellationToken) =>
        _ = await registration.GetAsync(cancellationToken).MatchAsync(read => Show(read, loadedNow: true, string.Empty), _ => Show(state, loadedNow: false, "Settings.Autostart.LoadFailed"));

    private async Task ToggleAsync(CancellationToken cancellationToken)
    {
        var change = IsEnabled ? registration.DisableAsync(cancellationToken) : registration.EnableAsync(cancellationToken);
        _ = await change.MatchAsync(changed => Show(changed, loadedNow: true, string.Empty), _ => Show(state, loadedNow: true, "Settings.Autostart.ChangeFailed"));
    }

    private Unit Show(AutostartState next, bool loadedNow, string key)
    {
        state = next;
        loaded = loadedNow;
        errorKey = key;
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(CanChange));
        OnPropertyChanged(nameof(ReasonText));
        OnPropertyChanged(nameof(ErrorText));
        ToggleCommand.NotifyCanExecuteChanged();

        return Unit.Instance;
    }

    private static string BlockerKey(AutostartBlocker blocker) => blocker is AutostartBlocker.NotInstalledBuild ? "Settings.Autostart.NotInstalled" : "Settings.Autostart.PlatformNotSupported";
}
