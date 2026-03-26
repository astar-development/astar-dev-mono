using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Logging;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class LogViewerViewModel : ReactiveObject
{
    private readonly InMemoryLogSink _sink;
    private readonly List<LogEntryViewModel> _allEntries = [];
    private LogEventLevel? _levelFilter;

    public LogViewerViewModel(InMemoryLogSink sink)
    {
        _sink = sink;
        _sink.LogEventReceived += OnLogEventReceived;

        ShowAllCommand = ReactiveCommand.Create(() => { _levelFilter = null; ApplyFilters(); });
        ShowErrorsCommand = ReactiveCommand.Create(() => { _levelFilter = LogEventLevel.Error; ApplyFilters(); });
        ShowWarningsCommand = ReactiveCommand.Create(() => { _levelFilter = LogEventLevel.Warning; ApplyFilters(); });
        ShowDebugCommand = ReactiveCommand.Create(() => { _levelFilter = LogEventLevel.Debug; ApplyFilters(); });
        ClearCommand = ReactiveCommand.Create(ClearEntries);
        CloseCommand = ReactiveCommand.Create(() => { });

        this.WhenAnyValue(x => x.SearchText).Subscribe(_ => ApplyFilters());
        this.WhenAnyValue(x => x.SelectedAccount).Subscribe(_ => ApplyFilters());
    }

    public ObservableCollection<LogEntryViewModel> FilteredEntries { get; } = [];
    public ObservableCollection<string> AccountLabels { get; } = [string.Empty];
    public bool HasEntries => FilteredEntries.Count > 0;
    public string EntryCountText => $"{FilteredEntries.Count} entries";

    public string SearchText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string SelectedAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public ICommand CloseCommand { get; init; }
    public ICommand ShowAllCommand { get; init; }
    public ICommand ShowErrorsCommand { get; init; }
    public ICommand ShowWarningsCommand { get; init; }
    public ICommand ShowDebugCommand { get; init; }
    public ICommand ClearCommand { get; init; }

    private void OnLogEventReceived(LogEvent logEvent)
    {
        var entry = ToViewModel(logEvent);
        Dispatcher.UIThread.Post(() =>
        {
            _allEntries.Add(entry);
            if (!string.IsNullOrEmpty(entry.AccountLabel) && !AccountLabels.Contains(entry.AccountLabel))
                AccountLabels.Add(entry.AccountLabel);

            if (PassesFilter(entry))
            {
                FilteredEntries.Add(entry);
                this.RaisePropertyChanged(nameof(HasEntries));
                this.RaisePropertyChanged(nameof(EntryCountText));
            }
        });
    }

    private void ClearEntries()
    {
        _allEntries.Clear();
        FilteredEntries.Clear();
        _sink.Clear();
        this.RaisePropertyChanged(nameof(HasEntries));
        this.RaisePropertyChanged(nameof(EntryCountText));
    }

    private void ApplyFilters()
    {
        FilteredEntries.Clear();
        foreach (var entry in _allEntries.Where(PassesFilter))
            FilteredEntries.Add(entry);
        this.RaisePropertyChanged(nameof(HasEntries));
        this.RaisePropertyChanged(nameof(EntryCountText));
    }

    private bool PassesFilter(LogEntryViewModel entry)
    {
        if (_levelFilter is { } level && entry.LogLevel != level)
            return false;
        if (!string.IsNullOrEmpty(SelectedAccount) && !string.Equals(entry.AccountLabel, SelectedAccount, StringComparison.Ordinal))
            return false;
        if (!string.IsNullOrWhiteSpace(SearchText) && !entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static LogEntryViewModel ToViewModel(LogEvent logEvent)
    {
        var account = logEvent.Properties.TryGetValue("AccountId", out var accountProp) ? accountProp.ToString().Trim('"') : string.Empty;
        var message = PiiSanitiser.Sanitise(logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture));

        return new LogEntryViewModel
        {
            TimestampText = logEvent.Timestamp.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            Level = logEvent.Level switch
            {
                LogEventLevel.Error => "ERR",
                LogEventLevel.Warning => "WRN",
                LogEventLevel.Information => "INF",
                LogEventLevel.Debug => "DBG",
                LogEventLevel.Verbose => "VRB",
                LogEventLevel.Fatal => "FTL",
                _ => "???"
            },
            LogLevel = logEvent.Level,
            AccountLabel = account,
            Message = message,
            LevelBackground = logEvent.Level switch
            {
                LogEventLevel.Error or LogEventLevel.Fatal => new SolidColorBrush(Color.FromRgb(220, 38, 38), 0.15),
                LogEventLevel.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8), 0.15),
                _ => Brushes.Transparent
            },
            LevelForeground = logEvent.Level switch
            {
                LogEventLevel.Error or LogEventLevel.Fatal => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                LogEventLevel.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8)),
                _ => Brushes.Gray
            }
        };
    }
}
