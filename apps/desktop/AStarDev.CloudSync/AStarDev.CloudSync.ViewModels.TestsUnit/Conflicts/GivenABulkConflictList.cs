using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public class GivenABulkConflictList
{
    private static readonly CloudAccount Photos = ConflictListFixture.Photos;
    private static readonly CloudAccount Work = ConflictListFixture.Work;

    [Fact]
    public async Task when_a_conflict_is_listed_then_its_tick_box_is_named_after_the_file_and_starts_unticked()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Camera/1.jpg"));

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        var item = fixture.List.Groups.Single().Items.Single();
        item.IsSelected.ShouldBeFalse();
        item.SelectAutomationName.ShouldBe("Select Camera/1.jpg");
        fixture.List.SelectedCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_items_are_ticked_then_the_selected_count_and_text_follow()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"), AttentionItems.CreateConflict(Photos.Id, "b.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        var items = fixture.List.Groups.Single().Items;

        items[0].IsSelected = true;
        items[1].IsSelected = true;

        fixture.List.SelectedCount.ShouldBe(2);
        fixture.List.SelectedText.ShouldBe("2 selected");

        items[1].IsSelected = false;

        fixture.List.SelectedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_select_all_is_used_then_every_item_of_the_current_filtered_list_is_ticked_and_hidden_items_are_not()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothAdded()), AttentionItems.CreateConflict(Photos.Id, "b.jpg", ConflictKindFactory.CreateBothModified()));
        fixture.Reader.Add(Work.Id, AttentionItems.CreateConflict(Work.Id, "c.docx", ConflictKindFactory.CreateBothAdded()));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == "Added on both sides");

        fixture.List.SelectAllCommand.Execute(null);

        fixture.List.Groups.SelectMany(group => group.Items).ShouldAllBe(item => item.IsSelected);
        fixture.List.SelectedCount.ShouldBe(2);

        fixture.List.SelectedFilter = fixture.List.Filters[0];

        fixture.List.SelectedCount.ShouldBe(2);
        fixture.List.Groups.SelectMany(group => group.Items).Count(item => item.IsSelected).ShouldBe(2);
        fixture.List.Groups.SelectMany(group => group.Items).Single(item => item.PathText == "b.jpg").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_filter_hides_a_ticked_item_then_it_is_no_longer_selected()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothAdded()), AttentionItems.CreateConflict(Photos.Id, "b.jpg", ConflictKindFactory.CreateBothModified()));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == "Added on both sides");
        fixture.List.SelectedFilter = fixture.List.Filters[0];

        fixture.List.SelectedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_selection_is_cleared_then_nothing_is_ticked()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        fixture.List.ClearSelectionCommand.Execute(null);

        fixture.List.SelectedCount.ShouldBe(0);
        fixture.List.Groups.Single().Items.Single().IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_list_is_reloaded_then_a_still_open_ticked_item_stays_ticked()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.Groups.Single().Items.Single().IsSelected.ShouldBeTrue();
        fixture.List.SelectedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_bulk_buttons_are_listed_then_there_is_one_per_strategy_with_a_name()
    {
        var fixture = new ConflictListFixture();

        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.BulkOptions.Select(option => option.Text).ShouldBe(["Keep local", "Keep cloud", "Keep both", "Skip"]);
        fixture.List.BulkOptions.Select(option => option.AutomationName).ShouldBe(["Apply Keep local to the selected conflicts", "Apply Keep cloud to the selected conflicts", "Apply Keep both to the selected conflicts", "Apply Skip to the selected conflicts"]);
    }

    [Fact]
    public async Task when_nothing_is_ticked_then_the_bulk_buttons_cannot_run()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.BulkOptions.ShouldAllBe(option => !option.Command.CanExecute(null));

        fixture.List.Groups.Single().Items.Single().IsSelected = true;

        fixture.List.BulkOptions.ShouldAllBe(option => option.Command.CanExecute(null));
        fixture.Resolver.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_bulk_strategy_is_applied_then_only_the_ticked_conflicts_are_resolved_one_call_per_account()
    {
        var fixture = new ConflictListFixture();
        var first = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        var untouched = AttentionItems.CreateConflict(Photos.Id, "b.jpg");
        var second = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "c.jpg");
        var third = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Work.Id, "d.docx");
        fixture.Reader.Add(Photos.Id, first, untouched, second);
        fixture.Reader.Add(Work.Id, third);
        fixture.Resolver.Outcome = (_, ids, _, _) =>
        {
            foreach (var id in ids)
                fixture.Reader.Remove(id);

            return Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(ids.Count, [], 0)));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        foreach (var item in fixture.List.Groups.SelectMany(group => group.Items).Where(item => item.PathText != "b.jpg"))
            item.IsSelected = true;

        await fixture.List.BulkOptions[1].Command.ExecuteAsync(null);

        fixture.Resolver.Calls.Count.ShouldBe(2);
        fixture.Resolver.Calls.ShouldAllBe(call => call.Strategy is ConflictStrategy.RemoteWins);
        fixture.Resolver.Calls.Single(call => call.AccountId == Photos.Id).Ids.ShouldBe([first.Id, second.Id], ignoreOrder: true);
        fixture.Resolver.Calls.Single(call => call.AccountId == Work.Id).Ids.ShouldBe([third.Id]);
        fixture.List.Groups.Single().Items.Select(item => item.PathText).ShouldBe(["b.jpg"]);
        fixture.List.SelectedCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_bulk_apply_fails_for_one_item_then_that_item_shows_its_error_and_stays_open_while_the_rest_close()
    {
        var fixture = new ConflictListFixture();
        var failing = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg");
        var closing = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "b.jpg");
        fixture.Reader.Add(Photos.Id, failing, closing);
        fixture.Resolver.Outcome = (_, _, _, _) =>
        {
            fixture.Reader.Remove(closing.Id);

            return Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(1, [FailedResolutionFactory.Create(failing.Id, ResolutionFailureFactory.CreateRemoteChanged())], 0)));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        await fixture.List.BulkOptions[0].Command.ExecuteAsync(null);

        var item = fixture.List.Groups.Single().Items.Single();
        item.PathText.ShouldBe("a.jpg");
        item.HasError.ShouldBeTrue();
        item.ErrorText.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_a_bulk_skip_is_applied_then_the_items_stay_listed_and_show_skipped()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"), AttentionItems.CreateConflict(Photos.Id, "b.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.Groups.Single().Items[0].IsSelected = true;

        await fixture.List.BulkOptions[3].Command.ExecuteAsync(null);

        var items = fixture.List.Groups.Single().Items;
        items[0].IsSkipped.ShouldBeTrue();
        items[1].IsSkipped.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_bulk_apply_is_refused_then_every_ticked_item_shows_the_refusal_and_stays_open()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"), AttentionItems.CreateConflict(Photos.Id, "b.jpg"));
        fixture.Resolver.Outcome = (_, _, _, _) => Task.FromResult(Result.Failure<ResolutionSummary, ErrorResponse>(new ErrorResponse("busy")));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        await fixture.List.BulkOptions[0].Command.ExecuteAsync(null);

        var items = fixture.List.Groups.Single().Items;
        items.ShouldAllBe(item => item.HasError && item.ErrorText.StartsWith("Could not resolve this now."));
        fixture.List.BulkErrorText.ShouldStartWith("Could not resolve this now.");
    }

    [Fact]
    public async Task when_a_bulk_apply_throws_then_the_items_show_the_unexpected_error_and_the_log_names_only_the_exception_type()
    {
        var logger = new RecordingLogger<AStarDev.CloudSync.ViewModels.Conflicts.ConflictListViewModel>();
        var fixture = new ConflictListFixture(logger);
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Secret/plan.docx"));
        fixture.Resolver.Outcome = (_, _, _, _) => throw new InvalidOperationException("C:/Users/me/Secret/plan.docx token=abc123 me@example.com");
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        await fixture.List.BulkOptions[0].Command.ExecuteAsync(null);

        fixture.List.Groups.Single().Items.Single().ErrorText.ShouldBe("Something went wrong, so nothing was resolved. The details have been logged.");
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
        logger.Messages.ShouldNotContain(message => message.Contains("Secret") || message.Contains("abc123") || message.Contains("example.com"));
    }

    [Fact]
    public async Task when_a_bulk_apply_is_started_twice_at_once_then_the_second_is_ignored()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        var release = new TaskCompletionSource();
        fixture.Resolver.Outcome = async (_, ids, _, _) =>
        {
            await release.Task;

            return Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(ids.Count, [], 0));
        };
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectAllCommand.Execute(null);

        var first = Task.Run(() => fixture.List.BulkOptions[0].Command.ExecuteAsync(null), TestContext.Current.CancellationToken);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => fixture.List.BulkOptions[0].Command.ExecuteAsync(null), TestContext.Current.CancellationToken)).Append(Task.Run(release.SetResult, TestContext.Current.CancellationToken)).Append(first));

        fixture.Resolver.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_every_kind_is_shown_then_there_is_no_default_strategy()
    {
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "a.jpg"));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.DefaultStrategy.Match(_ => true, () => false).ShouldBeFalse();
        fixture.List.DefaultCommand.CanExecute(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Changed on both sides", "KeepBoth")]
    [InlineData("Added on both sides", "KeepBoth")]
    [InlineData("Cloud copy changed during transfer", "KeepBoth")]
    [InlineData("Edited here, deleted in the cloud", "KeepBoth")]
    [InlineData("Deleted here, edited in the cloud", "KeepBoth")]
    [InlineData("Move target already taken", "Skip")]
    public async Task when_a_kind_is_shown_then_its_default_strategy_is_the_non_destructive_one(string filterText, string expected)
    {
        var fixture = new ConflictListFixture();
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);

        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == filterText);

        fixture.List.DefaultStrategy.Match(strategy => strategy.GetType().Name, () => "none").ShouldBe(expected);
    }

    [Fact]
    public async Task when_the_default_is_offered_then_nothing_runs_until_it_is_clicked_and_it_applies_to_the_ticked_conflicts_only()
    {
        var fixture = new ConflictListFixture();
        var ticked = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(Photos.Id, "a.jpg", ConflictKindFactory.CreateBothAdded());
        fixture.Reader.Add(Photos.Id, ticked, AttentionItems.CreateConflict(Photos.Id, "b.jpg", ConflictKindFactory.CreateBothAdded()));
        await fixture.List.LoadAsync(TestContext.Current.CancellationToken);
        fixture.List.SelectedFilter = fixture.List.Filters.Single(filter => filter.Text == "Added on both sides");
        fixture.List.Groups.Single().Items[0].IsSelected = true;

        fixture.Resolver.Calls.ShouldBeEmpty();
        fixture.List.DefaultText.ShouldBe("Apply the default for this kind (Keep both)");
        fixture.List.DefaultCommand.CanExecute(null).ShouldBeTrue();

        await fixture.List.DefaultCommand.ExecuteAsync(null);

        var call = fixture.Resolver.Calls.ShouldHaveSingleItem();
        call.Ids.ShouldBe([ticked.Id]);
        call.Strategy.ShouldBeOfType<ConflictStrategy.KeepBoth>();
    }
}
