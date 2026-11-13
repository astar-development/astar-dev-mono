namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenALocalChangeSetFactory
{
    [Fact]
    public void when_changes_are_given_unsorted_then_they_are_sorted_by_path_ignoring_case()
    {
        var set = LocalChangeSetFactory.Create([Deferred("b/x.txt"), Deferred("A/y.txt"), Deferred("a/z.txt")], 0, 0);

        string.Join(',', set.Changes.Select(change => change.Path.Value)).ShouldBe("A/y.txt,a/z.txt,b/x.txt");
    }

    [Fact]
    public void when_two_changes_name_the_same_path_ignoring_case_then_the_first_is_kept()
    {
        var set = LocalChangeSetFactory.Create([Deferred("Docs/a.txt"), LocalChangeFactory.CreateDeleted(LocalScanFixture.Path("docs/A.TXT"), RemoteIds.Of("item"))], 0, 0);

        set.Changes.Single().ShouldBeOfType<LocalChange.Deferred>();
    }

    [Fact]
    public void when_an_unreadable_change_shares_a_path_with_another_then_the_unreadable_one_wins()
    {
        var set = LocalChangeSetFactory.Create([Deferred("Docs/a.txt"), LocalChangeFactory.CreateUnreadable(LocalScanFixture.Path("docs/A.TXT"))], 0, 0);

        set.Changes.Single().ShouldBeOfType<LocalChange.Unreadable>();
    }

    [Fact]
    public void when_no_changes_are_given_then_the_set_is_empty_and_negative_counts_become_zero()
    {
        var set = LocalChangeSetFactory.Create(null!, -1, -2);

        (set.Changes.Count, set.Summary.Touched, set.Summary.Hashed).ShouldBe((0, 0, 0));
    }

    [Fact]
    public void when_a_change_has_a_negative_size_and_a_local_offset_then_it_is_normalised()
    {
        var added = LocalChangeFactory.CreateAdded(LocalScanFixture.Path("a.txt"), LocalItemKindFactory.CreateFile(), -4, new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.FromHours(2)));

        var change = added.ShouldBeOfType<LocalChange.Added>();
        (change.Size, change.LastWriteUtc).ShouldBe((0, new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero)));
    }

    private static LocalChange Deferred(string path) => LocalChangeFactory.CreateDeferred(LocalScanFixture.Path(path));
}
