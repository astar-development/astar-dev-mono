using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using ReactiveUI;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.LogViewer;

public sealed class GivenALogViewerViewModel : IDisposable
{
    private const string AnyAccountId     = "3057f494-687d-4abb-a653-4b8066230b6e";
    private const string OtherAccountId   = "aaaabbbb-cccc-dddd-eeee-ffffffffffff";
    private const string AllAccountsValue = "";

    private readonly IScheduler _originalScheduler      = RxApp.MainThreadScheduler;
    private readonly ILogEntryProvider _logProvider      = Substitute.For<ILogEntryProvider>();
    private readonly IAccountRepository _accountRepo     = Substitute.For<IAccountRepository>();
    private readonly IUserTypeService _userTypeService   = Substitute.For<IUserTypeService>();
    private readonly ILocalisationService _localisation  = Substitute.For<ILocalisationService>();
    private readonly Subject<LogEntry> _subject          = new();

    public GivenALogViewerViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _logProvider.EntryAdded.Returns(_subject);
        _logProvider.GetSnapshot().Returns([]);
        _accountRepo.GetAllAsync(default).ReturnsForAnyArgs(
            Task.FromResult<IReadOnlyList<Account>>([]));
        _userTypeService.CurrentUserType.Returns(UserType.Casual);
        _localisation.GetString(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
    }

    [Fact]
    public void when_log_entry_provider_is_null_then_throws()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new LogViewerViewModel(null!, _accountRepo, _userTypeService, _localisation));

        exception.ParamName.ShouldBe("logEntryProvider");
    }

    [Fact]
    public void when_account_repository_is_null_then_throws()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new LogViewerViewModel(_logProvider, null!, _userTypeService, _localisation));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void when_user_type_service_is_null_then_throws()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new LogViewerViewModel(_logProvider, _accountRepo, null!, _localisation));

        exception.ParamName.ShouldBe("userTypeService");
    }

    [Fact]
    public void when_localisation_service_is_null_then_throws()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new LogViewerViewModel(_logProvider, _accountRepo, _userTypeService, null!));

        exception.ParamName.ShouldBe("localisationService");
    }

    [Fact]
    public void when_user_is_casual_then_is_power_user_is_false()
    {
        _userTypeService.CurrentUserType.Returns(UserType.Casual);

        var sut = CreateSut();

        sut.IsPowerUser.ShouldBeFalse();
    }

    [Fact]
    public void when_user_is_power_user_then_is_power_user_is_true()
    {
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);

        var sut = CreateSut();

        sut.IsPowerUser.ShouldBeTrue();
    }

    [Fact]
    public void when_constructed_then_snapshot_entries_are_loaded()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Warning, "A warning", null)
        ]);

        var sut = CreateSut();

        sut.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void when_user_is_casual_then_debug_entries_are_filtered_out()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Debug, "Debug msg", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Warning, "Warning msg", null)
        ]);
        _userTypeService.CurrentUserType.Returns(UserType.Casual);

        var sut = CreateSut();

        sut.Entries.Count.ShouldBe(1);
        sut.Entries[0].Level.ShouldBe(LogEventLevel.Warning);
    }

    [Fact]
    public void when_user_is_casual_then_verbose_entries_are_filtered_out()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Verbose, "Verbose msg", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Error, "Error msg", null)
        ]);
        _userTypeService.CurrentUserType.Returns(UserType.Casual);

        var sut = CreateSut();

        sut.Entries.Count.ShouldBe(1);
        sut.Entries[0].Level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void when_user_is_power_user_then_all_levels_are_visible()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Verbose, "Verbose", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Debug, "Debug", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Info", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Warning, "Warning", null),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Error, "Error", null)
        ]);
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);

        var sut = CreateSut();

        sut.Entries.Count.ShouldBe(5);
    }

    [Fact]
    public void when_account_filter_set_then_only_matching_entries_are_visible()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Acc1 msg", AnyAccountId),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Acc2 msg", OtherAccountId),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "No acc msg", null)
        ]);
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);

        var sut = CreateSut();
        sut.SelectedAccountId = AnyAccountId;

        sut.Entries.Count.ShouldBe(1);
        sut.Entries[0].AccountId.ShouldBe(AnyAccountId);
    }

    [Fact]
    public void when_account_filter_is_all_then_all_account_entries_are_visible()
    {
        _logProvider.GetSnapshot().Returns([
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Acc1 msg", AnyAccountId),
            LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Acc2 msg", OtherAccountId)
        ]);
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);

        var sut = CreateSut();
        sut.SelectedAccountId = AllAccountsValue;

        sut.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public void when_new_entry_arrives_via_stream_then_entries_are_updated()
    {
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);
        var sut = CreateSut();

        _subject.OnNext(LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Live msg", null));

        sut.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void when_casual_user_and_info_entry_arrives_via_stream_then_entry_is_filtered()
    {
        _userTypeService.CurrentUserType.Returns(UserType.Casual);
        var sut = CreateSut();

        _subject.OnNext(LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "Info live", null));

        sut.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void when_log_file_path_requested_then_it_is_not_empty()
    {
        var sut = CreateSut();

        sut.LogFilePath.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_disposed_then_subsequent_stream_entries_do_not_update_collection()
    {
        _userTypeService.CurrentUserType.Returns(UserType.PowerUser);
        var sut = CreateSut();

        sut.Entries.ShouldBeEmpty();
        sut.Dispose();
        _subject.OnNext(LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "After dispose", null));

        sut.Entries.Count.ShouldBe(0);
    }

    [Fact]
    public void when_constructed_then_all_accounts_sentinel_is_first_filter_option()
    {
        var sut = CreateSut();

        sut.AccountFilterOptions[0].AccountId.ShouldBe(LogViewerViewModel.AllAccounts);
    }

    [Fact]
    public async Task when_accounts_are_loaded_then_filter_options_are_populated()
    {
        var account = new Account { Id = Guid.Parse(AnyAccountId), DisplayName = "Test Account" };
        _accountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<IReadOnlyList<Account>>([account]));

        var sut = CreateSut();

        await Task.Yield();

        sut.AccountFilterOptions.Count.ShouldBeGreaterThanOrEqualTo(2);
        sut.AccountFilterOptions.ShouldContain(opt => opt.AccountId == AnyAccountId);
    }

    public void Dispose()
    {
        RxApp.MainThreadScheduler = _originalScheduler;
        _subject.Dispose();
    }

    private LogViewerViewModel CreateSut() => new(_logProvider, _accountRepo, _userTypeService, _localisation);
}
