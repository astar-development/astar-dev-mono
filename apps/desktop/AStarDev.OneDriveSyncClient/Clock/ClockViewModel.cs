using AStarDev.ClockControls;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.Clock;

/// <summary>Exposes the clock style and seconds preference shown above the throughput graph, following the persisted settings.</summary>
public sealed class ClockViewModel : ObservableObject
{
    private ClockStyle style = ClockStyleFactory.Analogue();
    private bool showSeconds;

    /// <summary>Creates the view model from the current settings and follows later changes.</summary>
    public ClockViewModel(ISettingsService settingsService)
    {
        Apply(settingsService.Current);
        settingsService.SettingsChanged += (_, settings) => Apply(settings);
    }

    /// <summary>Gets a value indicating whether the analogue clock is shown.</summary>
    public bool IsAnalogue => style is AnalogueClockStyle;

    /// <summary>Gets a value indicating whether the digital clock is shown.</summary>
    public bool IsDigital => style is DigitalClockStyle;

    /// <summary>Gets a value indicating whether seconds are shown (digital seconds, analogue second hand).</summary>
    public bool ShowSeconds => showSeconds;

    private void Apply(AppSettings settings)
    {
        style = ClockStyleFactory.Create(settings.ClockStyle);
        showSeconds = settings.ShowClockSeconds;
        OnPropertyChanged(nameof(IsAnalogue));
        OnPropertyChanged(nameof(IsDigital));
        OnPropertyChanged(nameof(ShowSeconds));
    }
}
