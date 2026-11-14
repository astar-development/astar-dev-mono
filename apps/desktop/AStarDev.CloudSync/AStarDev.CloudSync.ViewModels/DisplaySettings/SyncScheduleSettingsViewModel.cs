using AStarDev.CloudSync.Sync;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The automatic sync interval choice (SE-04, #1403): a fixed list of intervals and manual only. A change is saved at once and the scheduler follows it without a restart.</summary>
public sealed class SyncScheduleSettingsViewModel : LocalizedObservableObject
{
    private readonly ISyncScheduleSettings settings;

    /// <summary>Creates the setting with the default schedule selected; call <see cref="LoadAsync"/> to show the saved one.</summary>
    /// <param name="settings">Loads and saves the schedule.</param>
    /// <param name="localization">Supplies the text.</param>
    public SyncScheduleSettingsViewModel(ISyncScheduleSettings settings, ILocalizationService localization) : base(localization)
    {
        this.settings = settings;
        Options = [];
    }

    /// <summary>The intervals, shortest first, then manual only.</summary>
    public IReadOnlyList<DisplayOptionViewModel> Options { get; }

    /// <summary>The group heading.</summary>
    public string HeadingText => throw new NotImplementedException($"{settings}");

    /// <summary>The message shown when the schedule could not be read or saved; empty when there is none.</summary>
    public string ErrorText => throw new NotImplementedException($"{settings}");

    /// <summary>Reads the saved schedule and selects it.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task LoadAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");

    /// <summary>Saves the schedule and selects it. When the save fails the previous choice stays and a message is shown.</summary>
    /// <param name="schedule">The schedule to use.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    public Task SelectAsync(SyncSchedule schedule, CancellationToken cancellationToken) => throw new NotImplementedException($"{schedule}{cancellationToken}");
}
