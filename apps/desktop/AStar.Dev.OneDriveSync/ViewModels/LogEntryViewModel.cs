using Avalonia.Media;
using ReactiveUI;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class LogEntryViewModel : ReactiveObject
{
    public string TimestampText { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public LogEventLevel LogLevel { get; init; }
    public string AccountLabel { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IBrush LevelBackground { get; init; } = Brushes.Transparent;
    public IBrush LevelForeground { get; init; } = Brushes.Gray;
}
