using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.Utilities;
using ReactiveUI;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>
///     View model for the Log Viewer (S014).
///     Reads from <see cref="ILogEntryProvider"/> — never reads log files from disk.
///     Filters by account and by user type (Casual: Warning+ only; Power User: all levels).
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase, IDisposable
{
    /// <summary>Sentinel value for "All Accounts" selection in <see cref="SelectedAccountId"/>.</summary>
    public const string AllAccounts = "";

    private readonly ILogEntryProvider _logEntryProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly IDisposable _subscription;
    private readonly List<LogEntry> _allEntries = [];

    private string _selectedAccountId = AllAccounts;

    /// <summary>Initialises the view model, pre-populates from snapshot, and subscribes to the live stream.</summary>
    public LogViewerViewModel(ILogEntryProvider logEntryProvider, IAccountRepository accountRepository, IUserTypeService userTypeService)
    {
        ArgumentNullException.ThrowIfNull(logEntryProvider);
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(userTypeService);

        _logEntryProvider  = logEntryProvider;
        _accountRepository = accountRepository;

        IsPowerUser  = userTypeService.CurrentUserType == UserType.PowerUser;
        LogFilePath  = ResolveLogFilePath();
        CopyPathCommand = ReactiveCommand.Create(CopyLogPath);

        foreach (var entry in logEntryProvider.GetSnapshot())
            _allEntries.Add(entry);

        ApplyFilters();

        _ = LoadAccountFilterOptionsAsync();

        _subscription = logEntryProvider.EntryAdded
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnEntryAdded);
    }

    /// <summary>Filtered and sorted log entries bound to the list view.</summary>
    public ObservableCollection<LogEntryViewModel> Entries { get; } = [];

    /// <summary>Account filter options: first item is "All Accounts", remainder are configured accounts.</summary>
    public ObservableCollection<AccountFilterOption> AccountFilterOptions { get; } = [new(AllAccounts, "All Accounts")];

    /// <summary>
    ///     The currently selected account filter.
    ///     Empty string means "All Accounts"; any other value is a synthetic account ID.
    /// </summary>
    public string SelectedAccountId
    {
        get => _selectedAccountId;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAccountId, value);
            ApplyFilters();
        }
    }

    /// <summary><see langword="true"/> when the current user type is Power User.</summary>
    public bool IsPowerUser { get; }

    /// <summary>Absolute path to the log directory (LG-07).</summary>
    public string LogFilePath { get; }

    /// <summary>Copies <see cref="LogFilePath"/> to the system clipboard.</summary>
    public ReactiveCommand<Unit, Unit> CopyPathCommand { get; }

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();

    private void OnEntryAdded(LogEntry entry)
    {
        _allEntries.Add(entry);

        if (!PassesFilters(entry))
            return;

        Entries.Insert(0, new LogEntryViewModel(entry));
    }

    private void ApplyFilters()
    {
        Entries.Clear();

        var filtered = _allEntries
            .Where(PassesFilters)
            .OrderByDescending(static entry => entry.Timestamp)
            .Select(static entry => new LogEntryViewModel(entry));

        foreach (var vm in filtered)
            Entries.Add(vm);
    }

    private bool PassesFilters(LogEntry entry) => PassesLevelFilter(entry) && PassesAccountFilter(entry);

    private bool PassesLevelFilter(LogEntry entry) =>
        IsPowerUser || entry.Level >= LogEventLevel.Warning;

    private bool PassesAccountFilter(LogEntry entry) =>
        _selectedAccountId == AllAccounts || entry.AccountId == _selectedAccountId;

    private async Task LoadAccountFilterOptionsAsync()
    {
        var accounts = await _accountRepository.GetAllAsync().ConfigureAwait(false);

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            foreach (var account in accounts)
                AccountFilterOptions.Add(new AccountFilterOption(account.Id.ToString(), account.DisplayName));
        });
    }

    private static void CopyLogPath()
    {
        string logPath = ResolveLogFilePath();

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            _ = window.Clipboard?.SetTextAsync(logPath);
    }

    private static string ResolveLogFilePath() =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            .CombinePath("AStar.Dev.OneDriveSync", "logs");
}
