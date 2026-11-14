using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public class GivenHeldDeletionsInTheConflictList
{
    private static readonly CloudAccount Photos = ConflictListFixture.Photos;
    private static readonly SyncPairId CameraRoll = new(Guid.NewGuid());

    [Fact]
    public async Task when_deletions_are_held_then_the_set_shows_account_pair_direction_count_and_paths()
    {
        var fixture = new ConflictListFixture();
        fixture.Pairs.Pairs = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([AttentionItems.Pair(Photos.Id, CameraRoll, "Camera roll")]);
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "b.jpg", "a.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var held = fixture.List.HeldDeletions.ShouldHaveSingleItem();
        held.HeaderText.ShouldBe("Photos · Camera roll");
        held.SummaryText.ShouldBe("2 files deleted in the cloud would be deleted here");
        held.PathTexts.ShouldBeEmpty();
        held.ToggleCommand.Execute(null);
        held.PathTexts.ShouldBe(["a.jpg", "b.jpg"]);
        fixture.List.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_direction_is_to_the_cloud_then_the_summary_says_so_and_one_file_is_singular()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteRemote(), "a.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.HeldDeletions.Single().SummaryText.ShouldBe("1 file deleted here would be deleted in the cloud");
    }

    [Fact]
    public async Task when_the_pair_is_unknown_or_its_read_fails_then_the_set_still_shows_with_a_fallback_name()
    {
        var fixture = new ConflictListFixture();
        fixture.Pairs.Pairs = _ => Result.Failure<IReadOnlyList<SyncPair>, ErrorResponse>(new ErrorResponse("down"));
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.HeldDeletions.Single().HeaderText.ShouldBe("Photos · Unknown folder");
    }

    [Fact]
    public async Task when_a_kind_filter_is_chosen_then_held_deletions_are_hidden_and_they_return_with_every_kind()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.SelectedFilter = fixture.List.Filters[1];

        fixture.List.HeldDeletions.ShouldBeEmpty();

        fixture.List.SelectedFilter = fixture.List.Filters[0];

        fixture.List.HeldDeletions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_paths_are_toggled_then_they_are_shown_and_hidden_again()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var held = fixture.List.HeldDeletions.Single();

        held.IsExpanded.ShouldBeFalse();
        held.ToggleText.ShouldBe("Show files");

        held.ToggleCommand.Execute(null);

        held.IsExpanded.ShouldBeTrue();
        held.ToggleText.ShouldBe("Hide files");

        held.ToggleCommand.Execute(null);

        held.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_buttons_are_named_then_the_names_hold_the_action_and_the_set()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var held = fixture.List.HeldDeletions.Single();

        held.ConfirmText.ShouldBe("Delete them");
        held.RestoreText.ShouldBe("Restore them");
        held.LaterText.ShouldBe("Decide later");
        held.ConfirmAutomationName.ShouldBe("Delete them: Photos · Unknown folder");
        held.RestoreAutomationName.ShouldBe("Restore them: Photos · Unknown folder");
        held.LaterAutomationName.ShouldBe("Decide later: Photos · Unknown folder");
        held.ToggleAutomationName.ShouldBe("Show files: Photos · Unknown folder");
    }

    [Fact]
    public async Task when_confirm_is_clicked_then_only_that_set_is_confirmed_and_it_leaves_the_list_once_closed()
    {
        var fixture = new ConflictListFixture();
        var chosen = (SyncAttentionItem.HeldDeletions)AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg");
        var other = AttentionItems.CreateHeld(Photos.Id, new SyncPairId(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteRemote(), "b.jpg");
        fixture.Reader.Add(Photos.Id, chosen, other);
        fixture.Decider.Outcome = (_, id, _) =>
        {
            fixture.Reader.RemoveHeld(id);

            return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.HeldDeletions.Single(held => held.Id == chosen.Id).ConfirmCommand.ExecuteAsync(null);

        var call = fixture.Decider.Calls.ShouldHaveSingleItem();
        call.Action.ShouldBe("Confirm");
        call.AccountId.ShouldBe(Photos.Id);
        call.Id.ShouldBe(chosen.Id);
        fixture.List.HeldDeletions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_restore_is_clicked_then_only_that_set_is_restored()
    {
        var fixture = new ConflictListFixture();
        var chosen = (SyncAttentionItem.HeldDeletions)AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteRemote(), "a.jpg");
        fixture.Reader.Add(Photos.Id, chosen, AttentionItems.CreateHeld(Photos.Id, new SyncPairId(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), "b.jpg"));
        fixture.Decider.Outcome = (_, id, _) =>
        {
            fixture.Reader.RemoveHeld(id);

            return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.HeldDeletions.Single(held => held.Id == chosen.Id).RestoreCommand.ExecuteAsync(null);

        var call = fixture.Decider.Calls.ShouldHaveSingleItem();
        call.Action.ShouldBe("Restore");
        call.Id.ShouldBe(chosen.Id);
        fixture.List.HeldDeletions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_decider_fails_then_the_set_stays_open_with_the_error_shown_after_a_reload()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        fixture.Decider.Outcome = (_, _, _) => Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("busy")));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.HeldDeletions.Single().ConfirmCommand.ExecuteAsync(null);

        var held = fixture.List.HeldDeletions.Single();
        held.HasError.ShouldBeTrue();
        held.ErrorText.ShouldBe("These deletions were not settled and are still held. A sync may be running for this account. Try again when it has finished.");
    }

    [Fact]
    public async Task when_the_decider_throws_then_the_set_shows_the_unexpected_error_and_the_log_names_only_the_exception_type()
    {
        var logger = new RecordingLogger<ConflictListViewModel>();
        var fixture = new ConflictListFixture(logger);
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "Secret/plan.docx"));
        fixture.Decider.Outcome = (_, _, _) => throw new InvalidOperationException("C:/Users/me/Secret/plan.docx token=abc123 me@example.com");
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        await fixture.List.HeldDeletions.Single().RestoreCommand.ExecuteAsync(null);

        fixture.List.HeldDeletions.Single().ErrorText.ShouldBe("Something went wrong, so nothing was resolved. The details have been logged.");
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
        logger.Messages.ShouldNotContain(message => message.Contains("Secret") || message.Contains("abc123") || message.Contains("example.com"));
    }

    [Fact]
    public async Task when_decide_later_is_clicked_then_nothing_runs_and_the_set_stays_held_and_marked_across_a_refresh()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.HeldDeletions.Single().LaterCommand.Execute(null);

        fixture.List.HeldDeletions.Single().IsDeferred.ShouldBeTrue();
        fixture.List.HeldDeletions.Single().DeferredText.ShouldBe("Held for now. It stays in the list and returns after a restart.");

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.HeldDeletions.Single().IsDeferred.ShouldBeTrue();
        fixture.Decider.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_confirm_is_started_twenty_times_at_once_then_the_decider_runs_once()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        var release = new TaskCompletionSource();
        fixture.Decider.Outcome = async (_, _, _) =>
        {
            await release.Task;

            return Result.Success<Unit, ErrorResponse>(Unit.Instance);
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var held = fixture.List.HeldDeletions.Single();

        var first = held.ConfirmCommand.ExecuteAsync(null);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => TryRun(held), TestContext.Current.CancellationToken)));
        release.SetResult();
        await first;

        fixture.Decider.Calls.Count.ShouldBe(1);
    }

    private static void TryRun(HeldDeletionItemViewModel held)
    {
        if (held.ConfirmCommand.CanExecute(null))
            held.ConfirmCommand.Execute(null);

        if (held.RestoreCommand.CanExecute(null))
            held.RestoreCommand.Execute(null);
    }

    [Fact]
    public async Task when_a_set_is_busy_then_its_buttons_cannot_run()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateHeld(Photos.Id, CameraRoll, DeletionDirectionFactory.CreateDeleteLocal(), "a.jpg"));
        var release = new TaskCompletionSource();
        var started = new TaskCompletionSource();
        fixture.Decider.Outcome = async (_, _, _) =>
        {
            started.SetResult();
            await release.Task;

            return Result.Success<Unit, ErrorResponse>(Unit.Instance);
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var held = fixture.List.HeldDeletions.Single();

        var running = held.ConfirmCommand.ExecuteAsync(null);
        await started.Task;

        held.IsBusy.ShouldBeTrue();
        held.ConfirmCommand.CanExecute(null).ShouldBeFalse();
        held.RestoreCommand.CanExecute(null).ShouldBeFalse();

        release.SetResult();
        await running;

        held.IsBusy.ShouldBeFalse();
    }
}
