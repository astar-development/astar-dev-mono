using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The sync trash retention choice (#1593): a fixed list of day counts. A change is saved at once and the next download pass uses it.</summary>
public sealed class TrashRetentionSettingsViewModel : LocalizedObservableObject
{
    private readonly ITrashRetentionSettings settings;
    private readonly IReadOnlyList<LocalTrashOptions> choices = [.. LocalTrashOptionsFactory.Choices.Select(LocalTrashOptionsFactory.CreateFromDays)];
    private LocalTrashOptions current = LocalTrashOptionsFactory.Create();
    private string errorKey = string.Empty;

    /// <summary>Creates the setting with the default selected; call <see cref="LoadAsync"/> to show the saved one.</summary>
    /// <param name="settings">Loads and saves the retention.</param>
    /// <param name="localization">Supplies the text.</param>
    public TrashRetentionSettingsViewModel(ITrashRetentionSettings settings, ILocalizationService localization) : base(localization)
    {
        this.settings = settings;
        Options = [.. choices.Select(choice => Option(choice, localization))];
        RefreshSelection();
    }

    /// <summary>The retentions, shortest first.</summary>
    public IReadOnlyList<DisplayOptionViewModel> Options { get; }

    /// <summary>The group heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.TrashRetention");

    /// <summary>The message shown when the retention could not be read or saved; empty when there is none.</summary>
    public string ErrorText => errorKey.Length == 0 ? string.Empty : Localization.GetLocal(errorKey);

    /// <summary>Reads the saved retention and selects it.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task LoadAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(cancellationToken).MatchAsync(saved => Apply(saved, string.Empty), _ => Show("Settings.TrashRetention.LoadFailed"));

    /// <summary>Saves the retention and selects it. When the save fails the previous choice stays and a message is shown.</summary>
    /// <param name="options">The retention to use.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    public Task SelectAsync(LocalTrashOptions options, CancellationToken cancellationToken) =>
        options == current ? Task.CompletedTask : settings.SetAsync(options, cancellationToken).MatchAsync(saved => Apply(saved, string.Empty), _ => Show("Settings.TrashRetention.SaveFailed"));

    private static string LabelKey(LocalTrashOptions options) => $"Settings.TrashRetention.Days{(int)options.Retention.TotalDays}";

    private DisplayOptionViewModel Option(LocalTrashOptions choice, ILocalizationService localization) => new(LabelKey(choice), () => _ = SelectAsync(choice, CancellationToken.None), localization);

    private Unit Apply(LocalTrashOptions options, string key)
    {
        current = options;
        errorKey = key;
        RefreshSelection();
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }

    private Unit Show(string key)
    {
        errorKey = key;
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }

    private void RefreshSelection()
    {
        for (var index = 0; index < Options.Count; index++)
            Options[index].IsSelected = choices[index] == current;
    }
}
