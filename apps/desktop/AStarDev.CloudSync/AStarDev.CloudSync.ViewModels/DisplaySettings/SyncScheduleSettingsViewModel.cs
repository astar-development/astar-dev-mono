using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The automatic sync interval choice (SE-04, #1403): a fixed list of intervals and manual only. A change is saved at once and the scheduler follows it without a restart.</summary>
public sealed class SyncScheduleSettingsViewModel : LocalizedObservableObject
{
    private readonly ISyncScheduleSettings settings;
    private readonly IReadOnlyList<(string Key, SyncSchedule Schedule)> choices;
    private SyncSchedule current = SyncScheduleFactory.CreateDefault();
    private string errorKey = string.Empty;

    /// <summary>Creates the setting with the default schedule selected; call <see cref="LoadAsync"/> to show the saved one.</summary>
    /// <param name="settings">Loads and saves the schedule.</param>
    /// <param name="localization">Supplies the text.</param>
    public SyncScheduleSettingsViewModel(ISyncScheduleSettings settings, ILocalizationService localization) : base(localization)
    {
        this.settings = settings;
        choices =
        [
            .. SyncScheduleFactory.Intervals.Select(interval => (IntervalKey(interval), SyncScheduleFactory.CreateEvery(interval))),
            ("Settings.Schedule.ManualOnly", SyncScheduleFactory.CreateManualOnly())
        ];
        Options = [.. choices.Select(choice => Option(choice.Key, choice.Schedule, localization))];
        RefreshSelection();
    }

    /// <summary>The intervals, shortest first, then manual only.</summary>
    public IReadOnlyList<DisplayOptionViewModel> Options { get; }

    /// <summary>The group heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.Schedule");

    /// <summary>The message shown when the schedule could not be read or saved; empty when there is none.</summary>
    public string ErrorText => errorKey.Length == 0 ? string.Empty : Localization.GetLocal(errorKey);

    /// <summary>Reads the saved schedule and selects it.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task LoadAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(cancellationToken).MatchAsync(saved => Apply(saved, string.Empty), _ => Show("Settings.Schedule.LoadFailed"));

    /// <summary>Saves the schedule and selects it. When the save fails the previous choice stays and a message is shown.</summary>
    /// <param name="schedule">The schedule to use.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    public Task SelectAsync(SyncSchedule schedule, CancellationToken cancellationToken) =>
        schedule == current ? Task.CompletedTask : settings.SetAsync(schedule, cancellationToken).MatchAsync(saved => Apply(saved, string.Empty), _ => Show("Settings.Schedule.SaveFailed"));

    private static string IntervalKey(TimeSpan interval) => interval.TotalMinutes switch
    {
        15 => "Settings.Schedule.Every15Minutes",
        30 => "Settings.Schedule.Every30Minutes",
        120 => "Settings.Schedule.Every2Hours",
        240 => "Settings.Schedule.Every4Hours",
        _ => "Settings.Schedule.EveryHour"
    };

    private DisplayOptionViewModel Option(string labelKey, SyncSchedule schedule, ILocalizationService localization) => new(labelKey, () => _ = SelectAsync(schedule, CancellationToken.None), localization);

    private Unit Apply(SyncSchedule schedule, string key)
    {
        current = schedule;
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
            Options[index].IsSelected = choices[index].Schedule == current;
    }
}
