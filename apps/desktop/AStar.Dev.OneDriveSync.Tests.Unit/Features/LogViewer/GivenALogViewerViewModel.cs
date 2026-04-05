using System.Collections.Generic;
using System.Reactive.Subjects;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using Microsoft.Reactive.Testing;
using ReactiveUI;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.LogViewer;

public sealed class GivenALogViewerViewModel : IDisposable
{
    private const string AnyAccountId     = "3057f494-687d-4abb-a653-4b8066230b6e";
    private const string OtherAccountId   = "aaaabbbb-cccc-dddd-eeee-ffffffffffff";
    private const string AllAccountsValue = "";

    private readonly IScheduler _originalMainScheduler = RxApp.MainThreadScheduler;
    private readonly ILogEntryProvider _logProvider     = Substitute.For<ILogEntryProvider>();
    private readonly IAccountRepository _accountRepo    = Substitute.For<IAccountRepository>();
    private readonly IUserTypeService _userTypeService  = Substitute.For<IUserTypeService>();
    private readonly Subject<LogEntry> _subject         = new();

    public GivenALogViewerViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _logProvider.EntryAdded.Returns(_subject);
        _logProvider.GetSnapshot().Returns([]);
        _accountRepo.GetAllAsync(default).ReturnsForAnyArgs(
            new Result<IReadOnlyList<Account>, string>.Ok([]));
        _userTypeService.CurrentUserType.Returns(UserType.Casual);
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

        sut.Dispose();
        _subject.OnNext(LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "After dispose", null));

        sut.Entries.ShouldBeEmpty();
    }

    public void Dispose()
    {
        RxApp.MainThreadScheduler = _originalMainScheduler;
        _subject.Dispose();
    }

    private LogViewerViewModel CreateSut() => new(_logProvider, _accountRepo, _userTypeService);
}
