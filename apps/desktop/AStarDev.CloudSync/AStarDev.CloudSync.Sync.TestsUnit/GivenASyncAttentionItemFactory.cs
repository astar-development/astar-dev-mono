using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncAttentionItemFactory
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void when_held_deletions_repeat_a_path_in_another_case_then_one_path_is_kept()
    {
        var item = Held(SyncPaths.Of("Docs/A.txt"), SyncPaths.Of("docs/a.TXT"), SyncPaths.Of("Docs/B.txt"));

        item.Paths.Count.ShouldBe(2);
    }

    [Fact]
    public void when_held_deletions_are_given_unsorted_then_they_are_sorted_ignoring_case()
    {
        var item = Held(SyncPaths.Of("b.txt"), SyncPaths.Of("A.txt"), SyncPaths.Of("c.txt"));

        string.Join(',', item.Paths.Select(path => path.Value)).ShouldBe("A.txt,b.txt,c.txt");
    }

    [Fact]
    public void when_held_deletions_include_a_default_path_then_it_is_dropped()
    {
        var item = Held(default, SyncPaths.Of("a.txt"));

        item.Paths.Count.ShouldBe(1);
    }

    [Fact]
    public void when_held_deletions_are_given_no_paths_then_the_list_is_empty()
    {
        var item = (SyncAttentionItem.HeldDeletions)SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), Account, SyncPairIdFactory.Create(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), null!, Moment);

        item.Paths.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_conflict_is_written_as_text_then_the_path_is_redacted()
    {
        var item = SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), Account, SyncPaths.Of("Secret/Plan.docx"), ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), Moment);

        item.ToString().ShouldNotContain("Secret");
    }

    [Fact]
    public void when_held_deletions_are_written_as_text_then_the_paths_are_redacted()
    {
        var item = Held(SyncPaths.Of("Secret/Plan.docx"));

        item.ToString().ShouldNotContain("Secret");
    }

    [Fact]
    public void when_a_present_side_has_a_negative_size_then_the_size_is_zero()
    {
        var side = (SideSnapshot.Present)SideSnapshotFactory.CreatePresent(-5, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());

        side.Size.ShouldBe(0);
    }

    [Fact]
    public void when_counts_are_negative_then_they_are_zero()
    {
        var counts = AttentionCountsFactory.Create(-1, -2, -3);

        (counts.Conflicts, counts.HeldDeletionSets, counts.HeldDeletionPaths).ShouldBe((0, 0, 0));
    }

    private static SyncAttentionItem.HeldDeletions Held(params RelativeSyncPath[] paths) =>
        (SyncAttentionItem.HeldDeletions)SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), Account, SyncPairIdFactory.Create(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), paths, Moment);
}
