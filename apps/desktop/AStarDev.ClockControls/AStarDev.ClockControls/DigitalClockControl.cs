using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AStarDev.ClockControls;

/// <summary>A text readout of the current time that updates every second.</summary>
[ExcludeFromCodeCoverage]
public class DigitalClockControl : TextBlock
{
    private readonly DispatcherTimer _timer;

    /// <summary>Defines the <see cref="ShowSeconds" /> property.</summary>
    public static readonly StyledProperty<bool> ShowSecondsProperty = AvaloniaProperty.Register<DigitalClockControl, bool>(nameof(ShowSeconds), true);

    /// <summary>Gets or sets a value indicating whether seconds are shown.</summary>
    public bool ShowSeconds
    {
        get => GetValue(ShowSecondsProperty);
        set => SetValue(ShowSecondsProperty, value);
    }

    /// <summary>Creates the control and shows the current time.</summary>
    public DigitalClockControl()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateText();
        UpdateText();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowSecondsProperty) UpdateText();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateText();
        if (!_timer.IsEnabled) _timer.Start();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_timer.IsEnabled) _timer.Stop();
    }

    private void UpdateText() => Text = DigitalClockFormatter.Format(DateTime.Now, ShowSeconds, CultureInfo.CurrentCulture);
}
