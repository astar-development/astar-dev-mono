using AStarDev.CloudSync.Sync;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The sync trash retention choice (#1593): a fixed list of day counts. A change is saved at once and the next download pass uses it.</summary>
public sealed class TrashRetentionSettingsViewModel : LocalizedObservableObject
{
    /// <summary>Creates the setting with the default selected; call <see cref="LoadAsync"/> to show the saved one.</summary>
    /// <param name="settings">Loads and saves the retention.</param>
    /// <param name="localization">Supplies the text.</param>
    public TrashRetentionSettingsViewModel(ITrashRetentionSettings settings, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{settings}");

    /// <summary>The retentions, shortest first.</summary>
    public IReadOnlyList<DisplayOptionViewModel> Options => throw new NotImplementedException();

    /// <summary>The group heading.</summary>
    public string HeadingText => throw new NotImplementedException();

    /// <summary>The message shown when the retention could not be read or saved; empty when there is none.</summary>
    public string ErrorText => throw new NotImplementedException();

    /// <summary>Reads the saved retention and selects it.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task LoadAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");

    /// <summary>Saves the retention and selects it. When the save fails the previous choice stays and a message is shown.</summary>
    /// <param name="options">The retention to use.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    public Task SelectAsync(LocalTrashOptions options, CancellationToken cancellationToken) => throw new NotImplementedException($"{options}{cancellationToken}");
}
