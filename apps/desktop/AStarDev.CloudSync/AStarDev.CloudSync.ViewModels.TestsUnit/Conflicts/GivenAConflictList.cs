using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public class GivenAConflictList
{
    private const string BusyMessage = "A sync pass is running for this account. Try again when it has finished.";

    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");

    [Fact]
    public async Task when_the_queue_is_empty_then_it_loads_as_empty_with_the_empty_text()
    {
        var fixture = new Fixture();

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.LoadState.ShouldBeOfType<SyncOverviewLoadState.Loaded>();
        fixture.List.IsEmpty.ShouldBeTrue();
        fixture.List.Groups.ShouldBeEmpty();
        fixture.List.EmptyText.ShouldBe("Nothing needs your attention.");
        fixture.List.LoadErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_conflicts_exist_for_two_accounts_then_they_are_grouped_by_account_and_accounts_without_conflicts_are_left_out()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Work.Id, AttentionItems.CreateConflict(Work.Id, "Docs/a.docx"));
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Camera/1.jpg"), AttentionItems.CreateConflict(Photos.Id, "Camera/2.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.IsEmpty.ShouldBeFalse();
        fixture.List.Groups.Select(group => group.HeaderText).ShouldBe(["Photos · 2 conflicts", "Work · 1 conflict"]);
        fixture.List.Groups[0].Items.Select(item => item.PathText).ShouldBe(["Camera/1.jpg", "Camera/2.jpg"]);
    }

    [Fact]
    public async Task when_the_queue_holds_held_deletions_then_they_are_not_listed()
    {
        var fixture = new Fixture();
        var held = SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), Photos.Id, new SyncPairId(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), [AttentionItems.Path("a.txt")], TestData.Now);
        fixture.Reader.Add(Photos.Id, held);

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.Groups.ShouldBeEmpty();
        fixture.List.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void when_created_then_the_filters_are_every_kind_then_each_kind()
    {
        var fixture = new Fixture();

        fixture.List.Filters.Select(filter => filter.Text).ShouldBe(["All kinds", "Changed on both sides", "Edited here, deleted in the cloud", "Deleted here, edited in the cloud", "Added on both sides", "Move target already taken", "Cloud copy changed during transfer"]);
        fixture.List.SelectedFilter.ShouldBeSameAs(fixture.List.Filters[0]);
    }

    [Fact]
    public async Task when_a_kind_is_selected_then_only_conflicts_of_that_kind_are_listed_and_all_kinds_lists_everything_again()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothModified()), AttentionItems.CreateConflict(Photos.Id, "b.jpg", ConflictKindFactory.CreateBothAdded()));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == "Added on both sides");

        fixture.List.Groups.Single().Items.Select(item => item.PathText).ShouldBe(["b.jpg"]);

        fixture.List.SelectedFilter = fixture.List.Filters[0];

        fixture.List.Groups.Single().Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_filter_hides_every_conflict_then_the_list_shows_the_empty_text_and_no_group()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothModified()));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == "Added on both sides");

        fixture.List.Groups.ShouldBeEmpty();
        fixture.List.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_conflict_is_listed_then_it_shows_its_kind_and_detection_time_and_offers_the_four_strategies()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Camera/1.jpg", ConflictKindFactory.CreateBothAdded()));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var item = fixture.List.Groups.Single().Items.Single();
        item.KindText.ShouldBe("Added on both sides");
        item.DetectedText.ShouldBe("Detected 03/11/2026 06:00 UTC");
        item.Options.Select(option => option.Text).ShouldBe(["Keep local", "Keep cloud", "Keep both", "Skip"]);
        item.Options.ShouldAllBe(option => option.IsAllowed && option.DisabledReasonText == string.Empty && option.Command.CanExecute(null));
        item.UnavailableText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_local_side_is_absent_then_the_strategies_that_need_it_are_disabled_with_the_reason()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothModified(), SideSnapshotFactory.CreateAbsent(), AttentionItems.Present()));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var options = fixture.List.Groups.Single().Items.Single().Options;
        options.Select(option => option.IsAllowed).ShouldBe([false, true, false, true]);
        options[0].DisabledReasonText.ShouldBe("Not available: the local file is missing.");
        options[0].Command.CanExecute(null).ShouldBeFalse();
        options[2].Command.CanExecute(null).ShouldBeFalse();
        fixture.List.Groups.Single().Items.Single().UnavailableText.ShouldBe("Not available: the local file is missing.");
    }

    [Fact]
    public async Task when_the_cloud_side_is_absent_then_the_strategies_that_need_it_are_disabled_with_the_reason()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothModified(), AttentionItems.Present(), SideSnapshotFactory.CreateAbsent()));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var options = fixture.List.Groups.Single().Items.Single().Options;
        options.Select(option => option.IsAllowed).ShouldBe([false, false, false, true]);
        options[1].DisabledReasonText.ShouldBe("Not available: the cloud file is missing.");
    }

    [Fact]
    public async Task when_a_strategy_button_is_named_then_the_name_holds_the_strategy_and_the_file()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Camera/1.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.Groups.Single().Items.Single().Options.Select(option => option.AutomationName).ShouldBe(["Keep local: Camera/1.jpg", "Keep cloud: Camera/1.jpg", "Keep both: Camera/1.jpg", "Skip: Camera/1.jpg"]);
    }

    [Theory]
    [InlineData(0, "LocalWins")]
    [InlineData(1, "RemoteWins")]
    [InlineData(2, "KeepBoth")]
    public async Task when_a_strategy_is_chosen_then_only_that_conflict_is_resolved_with_it_and_it_leaves_the_list_once_closed(int button, string expected)
    {
        var fixture = new Fixture();
        var chosen = AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        var other = AttentionItems.CreateConflict(Photos.Id, "b.jpg");
        fixture.Reader.Add(Photos.Id, chosen, other);
        fixture.Resolver.Outcome = (_, ids, _, _) =>
        {
            foreach (var id in ids)
                fixture.Reader.Remove(id);

            return Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(ids.Count, [], 0)));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items[0].Options[button].Command.ExecuteAsync(null);

        var call = fixture.Resolver.Calls.ShouldHaveSingleItem();
        call.AccountId.ShouldBe(Photos.Id);
        call.Ids.ShouldBe([((SyncAttentionItem.Conflict)chosen).Id]);
        call.Strategy.GetType().Name.ShouldBe(expected);
        fixture.List.Groups.Single().Items.Select(item => item.PathText).ShouldBe(["b.jpg"]);
    }

    [Fact]
    public async Task when_the_last_conflict_is_resolved_then_the_list_shows_the_empty_text()
    {
        var fixture = new Fixture();
        var only = AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        fixture.Reader.Add(Photos.Id, only);
        fixture.Resolver.Outcome = (_, ids, _, _) =>
        {
            foreach (var id in ids)
                fixture.Reader.Remove(id);

            return Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(1, [], 0)));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);

        fixture.List.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_transfer_failed_then_the_conflict_stays_listed_with_the_error_and_the_error_survives_a_refresh()
    {
        var fixture = new Fixture();
        var conflict = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        fixture.Reader.Add(Photos.Id, conflict);
        fixture.Resolver.Outcome = (_, _, _, _) => Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(0, [FailedResolutionFactory.Create(conflict.Id, ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateTransient()))], 0)));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);
        await fixture.List.RefreshCommand.ExecuteAsync(null);

        var item = fixture.List.Groups.Single().Items.Single();
        item.HasError.ShouldBeTrue();
        item.ErrorText.ShouldBe("The cloud provider reported an error, so nothing was resolved. Try again.");
        item.IsBusy.ShouldBeFalse();
        item.Options[0].Command.CanExecute(null).ShouldBeTrue();
    }

    [Theory]
    [InlineData("NotAllowed", "This choice does not apply to this conflict.")]
    [InlineData("NotOpen", "This conflict is no longer in the list. Refresh the list.")]
    [InlineData("Unsupported", "This kind of conflict cannot be resolved this way yet.")]
    [InlineData("LocalFileMissing", "The local file is missing, so nothing was changed.")]
    [InlineData("RemoteFileMissing", "The cloud file is missing, so nothing was changed.")]
    [InlineData("LocalChanged", "The local file changed while it was transferred, so nothing was resolved. Try again.")]
    [InlineData("RemoteChanged", "The cloud file changed after this conflict was shown, so nothing was resolved. Refresh the list and try again.")]
    [InlineData("Local", "The file system reported an error, so nothing was resolved. Try again.")]
    [InlineData("NotRecorded", "The transfer succeeded but the conflict could not be closed. Refresh the list.")]
    [InlineData("Unexpected", "Something went wrong, so nothing was resolved. The details have been logged.")]
    public async Task when_a_failure_reason_is_reported_then_the_error_text_says_it_in_words(string reason, string expected)
    {
        var fixture = new Fixture();
        var conflict = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        fixture.Reader.Add(Photos.Id, conflict);
        fixture.Resolver.Outcome = (_, _, _, _) => Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(0, [FailedResolutionFactory.Create(conflict.Id, Failure(reason))], 0)));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);

        fixture.List.Groups.Single().Items.Single().ErrorText.ShouldBe(expected);
    }

    [Fact]
    public async Task when_the_account_is_busy_then_the_refusal_is_shown_on_the_conflict_which_stays_listed()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        fixture.Resolver.Outcome = (_, _, _, _) => Task.FromResult(Result.Failure<ResolutionSummary, ErrorResponse>(new ErrorResponse(BusyMessage)));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);

        fixture.List.Groups.Single().Items.Single().ErrorText.ShouldBe("Could not resolve this now. A sync may be running for this account, or sync is paused for an update. Try again when it has finished.");
    }

    [Fact]
    public async Task when_a_later_attempt_succeeds_then_the_error_is_gone_with_the_conflict()
    {
        var fixture = new Fixture();
        var conflict = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        fixture.Reader.Add(Photos.Id, conflict);
        fixture.Resolver.Outcome = (_, _, _, _) => Task.FromResult(Result.Failure<ResolutionSummary, ErrorResponse>(new ErrorResponse(BusyMessage)));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);
        fixture.Resolver.Outcome = (_, ids, _, _) =>
        {
            foreach (var id in ids)
                fixture.Reader.Remove(id);

            return Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(1, [], 0)));
        };

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);

        fixture.List.Groups.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_skip_is_chosen_then_the_resolver_is_asked_and_the_conflict_stays_listed_marked_as_skipped()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[3].Command.ExecuteAsync(null);
        await fixture.List.RefreshCommand.ExecuteAsync(null);

        fixture.Resolver.Calls.ShouldHaveSingleItem().Strategy.ShouldBeOfType<ConflictStrategy.Skip>();
        var item = fixture.List.Groups.Single().Items.Single();
        item.IsSkipped.ShouldBeTrue();
        item.SkippedText.ShouldBe("Skipped for now. It stays in the list and returns after a restart.");
        item.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_resolver_throws_then_the_conflict_shows_the_unexpected_error_and_the_log_names_only_the_exception_type()
    {
        var logger = new RecordingLogger<ConflictListViewModel>();
        var fixture = new Fixture(logger);
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Secret/plan.docx"));
        fixture.Resolver.Outcome = (_, _, _, _) => throw new InvalidOperationException("C:/Users/me/Secret/plan.docx token=abc123 me@example.com");
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.Groups.Single().Items.Single().Options[0].Command.ExecuteAsync(null);

        fixture.List.Groups.Single().Items.Single().ErrorText.ShouldBe("Something went wrong, so nothing was resolved. The details have been logged.");
        logger.Entries.ShouldNotBeEmpty();
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
        logger.Messages.ShouldNotContain(message => message.Contains("Secret") || message.Contains("abc123") || message.Contains("example.com"));
    }

    [Fact]
    public async Task when_the_user_cancels_the_resolution_then_no_error_is_shown_and_the_conflict_can_be_tried_again()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        fixture.Resolver.Outcome = (_, _, _, _) => throw new OperationCanceledException();
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var item = fixture.List.Groups.Single().Items.Single();
        _ = await Task.WhenAny(item.Options[0].Command.ExecuteAsync(null));

        item.HasError.ShouldBeFalse();
        item.IsBusy.ShouldBeFalse();
        item.Options[0].Command.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task when_two_buttons_are_pressed_at_once_then_the_resolver_runs_once_and_the_buttons_are_disabled_meanwhile()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        var release = new TaskCompletionSource<Result<ResolutionSummary, ErrorResponse>>();
        fixture.Resolver.Outcome = (_, _, _, _) => release.Task;
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var item = fixture.List.Groups.Single().Items.Single();

        var first = item.Options[0].Command.ExecuteAsync(null);
        var second = item.Options[1].Command.ExecuteAsync(null);

        item.IsBusy.ShouldBeTrue();
        item.Options.ShouldAllBe(option => !option.Command.CanExecute(null));
        fixture.Resolver.Calls.Count.ShouldBe(1);
        release.SetResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(1, [], 0)));
        await Task.WhenAll(first, second);
        fixture.Resolver.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_one_account_cannot_be_read_then_the_load_fails_and_the_other_accounts_conflicts_are_not_shown_as_the_whole_queue()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        fixture.Reader.Fails = accountId => accountId == Work.Id ? new ErrorResponse("database C:/secret/cloudsync.db is locked") : null;

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        fixture.List.IsEmpty.ShouldBeFalse();
        fixture.List.Groups.ShouldBeEmpty();
        fixture.List.LoadErrorText.ShouldBe("Could not read the conflicts. The details have been logged.");
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_load_fails()
    {
        var fixture = new Fixture(accounts: new FailingAccountsSource());

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        fixture.List.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task when_reading_throws_then_the_load_fails_and_the_log_names_only_the_exception_type()
    {
        var logger = new RecordingLogger<ConflictListViewModel>();
        var fixture = new Fixture(logger);
        fixture.Reader.Throws = _ => new InvalidOperationException("C:/Users/me/cloudsync.db me@example.com");

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
        logger.Messages.ShouldNotContain(message => message.Contains("cloudsync") || message.Contains("example.com"));
    }

    [Fact]
    public async Task when_the_load_is_cancelled_then_the_cancellation_is_not_swallowed()
    {
        var fixture = new Fixture();
        fixture.Reader.Throws = _ => new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() => fixture.List.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_the_queue_is_read_twenty_times_at_once_then_the_list_is_consistent()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"), AttentionItems.CreateConflict(Photos.Id, "b.jpg"));
        fixture.Reader.Add(Work.Id, AttentionItems.CreateConflict(Work.Id, "c.jpg"));

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => fixture.List.LoadAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));

        fixture.List.Groups.Select(group => group.Items.Count).ShouldBe([2, 1]);
        fixture.List.LoadState.ShouldBeOfType<SyncOverviewLoadState.Loaded>();
    }

    [Fact]
    public async Task when_opened_then_the_screen_shows_and_reads_the_queue_and_when_closed_it_hides()
    {
        var fixture = new Fixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        var changed = new List<string?>();
        fixture.List.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await fixture.List.OpenCommand.ExecuteAsync(null);

        fixture.List.IsOpen.ShouldBeTrue();
        fixture.List.Groups.Count.ShouldBe(1);
        changed.ShouldContain(nameof(ConflictListViewModel.IsOpen));

        fixture.List.CloseCommand.Execute(null);

        fixture.List.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_created_then_the_static_text_is_localised()
    {
        var fixture = new Fixture();

        string[] texts = [fixture.List.TitleText, fixture.List.ReviewText, fixture.List.CloseText, fixture.List.RefreshText, fixture.List.FilterLabelText];

        texts.ShouldBe(["Needs your attention", "Review conflicts", "Close", "Refresh", "Show"]);
    }

    private static ResolutionFailure Failure(string reason) => reason switch
    {
        "NotAllowed" => ResolutionFailureFactory.CreateNotAllowed(ResolutionNotAllowedFactory.CreateLocalSideAbsent()),
        "NotOpen" => ResolutionFailureFactory.CreateNotOpen(),
        "Unsupported" => ResolutionFailureFactory.CreateUnsupported(),
        "LocalFileMissing" => ResolutionFailureFactory.CreateLocalFileMissing(),
        "RemoteFileMissing" => ResolutionFailureFactory.CreateRemoteFileMissing(),
        "LocalChanged" => ResolutionFailureFactory.CreateLocalChanged(),
        "RemoteChanged" => ResolutionFailureFactory.CreateRemoteChanged(),
        "Local" => ResolutionFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateLocked()),
        "NotRecorded" => ResolutionFailureFactory.CreateNotRecorded(),
        _ => ResolutionFailureFactory.CreateUnexpected(),
    };

    private sealed class FailingAccountsSource : AStarDev.CloudSync.Overview.ISyncOverviewSource
    {
        public Task<Result<AStarDev.CloudSync.Overview.SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<AStarDev.CloudSync.Overview.SyncOverview, ErrorResponse>(new ErrorResponse("database is locked")));
    }

    private sealed class Fixture
    {
        public Fixture() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<ConflictListViewModel>.Instance, new FixedAccountsSource(Photos, Work))
        {
        }

        public Fixture(Microsoft.Extensions.Logging.ILogger<ConflictListViewModel> logger) : this(logger, new FixedAccountsSource(Photos, Work))
        {
        }

        public Fixture(AStarDev.CloudSync.Overview.ISyncOverviewSource accounts) : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<ConflictListViewModel>.Instance, accounts)
        {
        }

        private Fixture(Microsoft.Extensions.Logging.ILogger<ConflictListViewModel> logger, AStarDev.CloudSync.Overview.ISyncOverviewSource accounts)
        {
            List = TestData.CreateConflictList(accounts, Reader, Resolver, logger);
        }

        public FakeAttentionReader Reader { get; } = new();

        public FakeConflictResolver Resolver { get; } = new();

        public ConflictListViewModel List { get; }
    }
}
