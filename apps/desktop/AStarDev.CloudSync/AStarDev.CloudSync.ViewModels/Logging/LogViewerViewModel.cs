using System.Globalization;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Logging;

/// <summary>The hidden log viewer (#1411): every account's local log lines in one list, filtered by account, level and text. Open it with <see cref="Refresh"/> (the files are read once; changing a filter only re-filters what was read).</summary>
public sealed class LogViewerViewModel : LocalizedObservableObject
{
    private static readonly string[] LevelKeys = ["LogViewer.Level.All", "LogViewer.Level.Information", "LogViewer.Level.Warning", "LogViewer.Level.Error"];
    private static readonly LogLevel[] Levels = [LogLevel.Trace, LogLevel.Information, LogLevel.Warning, LogLevel.Error];

    private readonly ILogEntryReader reader;
    private readonly SyncOverviewViewModel overview;
    private readonly ILogDiagnosticsClipboard clipboard;
    private IReadOnlyList<LogLine> everything = [];
    private IReadOnlyList<string> lines = [];
    private LogFilterOption<Option<string>>? account;
    private LogFilterOption<LogLevel>? level;
    private string searchText = string.Empty;
    private IReadOnlyList<LogFilterOption<Option<string>>> accountOptions;
    private IReadOnlyList<LogFilterOption<LogLevel>> levelOptions;

    /// <summary>Creates the viewer; call <see cref="RefreshAsync"/> to read the files.</summary>
    /// <param name="reader">Reads the local log files.</param>
    /// <param name="overview">Supplies the accounts for the account filter.</param>
    /// <param name="clipboard">Copies the diagnostics.</param>
    /// <param name="localization">Supplies the text.</param>
    public LogViewerViewModel(ILogEntryReader reader, SyncOverviewViewModel overview, ILogDiagnosticsClipboard clipboard, ILocalizationService localization) : base(localization)
    {
        this.reader = reader;
        this.overview = overview;
        this.clipboard = clipboard;
        accountOptions = BuildAccountOptions();
        levelOptions = BuildLevelOptions();
    }

    /// <summary>The window title.</summary>
    public string TitleText => Localization.GetLocal("LogViewer.Title");

    /// <summary>The label of the account filter.</summary>
    public string AccountText => Localization.GetLocal("LogViewer.Account");

    /// <summary>The label of the level filter.</summary>
    public string LevelText => Localization.GetLocal("LogViewer.Level");

    /// <summary>The label of the search box.</summary>
    public string SearchLabelText => Localization.GetLocal("LogViewer.Search");

    /// <summary>The text of the reload button.</summary>
    public string RefreshText => Localization.GetLocal("LogViewer.Refresh");

    /// <summary>The text of the copy button.</summary>
    public string CopyText => Localization.GetLocal("LogViewer.Copy");

    /// <summary>The accounts to choose from, with "all accounts" first.</summary>
    public IReadOnlyList<LogFilterOption<Option<string>>> AccountOptions => accountOptions;

    /// <summary>The levels to choose from.</summary>
    public IReadOnlyList<LogFilterOption<LogLevel>> LevelOptions => levelOptions;

    /// <summary>The chosen account; all accounts until one is chosen.</summary>
    public LogFilterOption<Option<string>> SelectedAccount
    {
        get => account ?? AccountOptions[0];
        set
        {
            if (SetProperty(ref account, value))
                Filter();
        }
    }

    /// <summary>The chosen level; everything until one is chosen.</summary>
    public LogFilterOption<LogLevel> SelectedLevel
    {
        get => level ?? LevelOptions[0];
        set
        {
            if (SetProperty(ref level, value))
                Filter();
        }
    }

    /// <summary>The text to find in the message or category.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value ?? string.Empty))
                Filter();
        }
    }

    /// <summary>The lines that match the filters, oldest first.</summary>
    public IReadOnlyList<string> Lines => lines;

    /// <summary>How many lines are shown, or the reason there are none.</summary>
    public string StatusText => Localization.GetLocal("LogViewer.Count", lines.Count);

    /// <summary>Reads the files again.</summary>
    public IAsyncRelayCommand RefreshCommand => field ??= new AsyncRelayCommand(RefreshAsync);

    /// <summary>Copies the lines that are shown, which hold no personal data, to the clipboard.</summary>
    public IAsyncRelayCommand CopyDiagnosticsCommand => field ??= new AsyncRelayCommand(CopyAsync);

    /// <summary>Reads every log file and shows the lines that match the filters.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        accountOptions = BuildAccountOptions();
        OnPropertyChanged(nameof(AccountOptions));
        var read = await Task.Run(async () =>
        {
            var found = new List<LogLine>();
            await foreach (var line in reader.ReadAsync(LogQueryFactory.CreateEverything(), cancellationToken))
                found.Add(line);

            return found;
        }, cancellationToken);
        everything = read;
        Filter();
    }

    /// <inheritdoc/>
    protected override void OnCultureChanged()
    {
        accountOptions = BuildAccountOptions();
        levelOptions = BuildLevelOptions();
        account = null;
        level = null;
        base.OnCultureChanged();
    }

    private async Task CopyAsync(CancellationToken cancellationToken) => await clipboard.SetTextAsync(string.Join('\n', lines), cancellationToken);

    private void Filter()
    {
        var query = LogQueryFactory.Create(SelectedAccount.Value, SelectedLevel.Value, searchText);
        lines = [.. everything.Where(query.Matches).Select(Format)];
        OnPropertyChanged(nameof(Lines));
        OnPropertyChanged(nameof(StatusText));
    }

    private List<LogFilterOption<Option<string>>> BuildAccountOptions() => [LogFilterOptionFactory.Create(Option.None<string>(), Localization.GetLocal("LogViewer.AllAccounts")), .. overview.Accounts.Select(item => LogFilterOptionFactory.Create(Option.Some(LogHash.Of(item.Id.Value.ToString())), item.Name))];

    private List<LogFilterOption<LogLevel>> BuildLevelOptions() => [.. Levels.Select((value, index) => LogFilterOptionFactory.Create(value, Localization.GetLocal(LevelKeys[index])))];

    private static string Format(LogLine line) => string.Create(CultureInfo.InvariantCulture, $"{line.Time:yyyy-MM-dd HH:mm:ss.fff} {line.Level,-11} {line.Category}: {line.Message}");
}
