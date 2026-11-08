namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAQuarantineSnapshot
{
    [Fact]
    public void when_a_path_has_no_entry_then_it_is_clear()
    {
        var snapshot = QuarantineSnapshotFactory.CreateEmpty();

        snapshot.StateOf(SyncPaths.Of("Documents/Report.docx")).ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public void when_a_path_is_conflicted_then_a_casing_variant_of_it_is_conflicted()
    {
        var snapshot = Snapshot(("Documents/Report.docx", PathQuarantineStateFactory.CreateConflicted()));

        snapshot.StateOf(SyncPaths.Of("documents/REPORT.docx")).ShouldBe(PathQuarantineStateFactory.CreateConflicted());
    }

    [Fact]
    public void when_two_entries_differ_only_in_case_then_the_first_non_clear_one_is_kept()
    {
        var snapshot = Snapshot(("Photos/Beach.jpg", PathQuarantineStateFactory.CreateHeldByGuard()), ("photos/beach.JPG", PathQuarantineStateFactory.CreateConflicted()));

        (snapshot.Entries.Count, snapshot.StateOf(SyncPaths.Of("Photos/Beach.jpg"))).ShouldBe((1, PathQuarantineStateFactory.CreateHeldByGuard()));
    }

    [Fact]
    public void when_a_clear_entry_comes_before_a_held_casing_variant_then_the_held_state_wins()
    {
        var snapshot = Snapshot(("Docs/A.txt", PathQuarantineStateFactory.CreateClear()), ("docs/a.txt", PathQuarantineStateFactory.CreateHeldByGuard()));

        snapshot.StateOf(SyncPaths.Of("Docs/A.txt")).ShouldBe(PathQuarantineStateFactory.CreateHeldByGuard());
    }

    [Fact]
    public void when_a_held_entry_comes_before_a_clear_casing_variant_then_the_held_state_wins()
    {
        var snapshot = Snapshot(("Docs/A.txt", PathQuarantineStateFactory.CreateHeldByGuard()), ("docs/a.txt", PathQuarantineStateFactory.CreateClear()));

        snapshot.StateOf(SyncPaths.Of("docs/A.TXT")).ShouldBe(PathQuarantineStateFactory.CreateHeldByGuard());
    }

    [Theory]
    [InlineData("Photos/2024/Beach.jpg")]
    [InlineData("photos/2024/beach.jpg")]
    [InlineData("Photos")]
    public void when_a_folder_is_held_then_everything_under_it_is_held(string value)
    {
        var snapshot = Snapshot(("Photos", PathQuarantineStateFactory.CreateHeldByGuard()));

        snapshot.StateOf(SyncPaths.Of(value)).ShouldBe(PathQuarantineStateFactory.CreateHeldByGuard());
    }

    [Fact]
    public void when_a_folder_is_held_then_a_sibling_sharing_its_name_prefix_is_clear()
    {
        var snapshot = Snapshot(("Photos", PathQuarantineStateFactory.CreateHeldByGuard()));

        snapshot.StateOf(SyncPaths.Of("Photoshop/Banner.psd")).ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public void when_a_file_is_conflicted_inside_a_resyncing_folder_then_the_files_own_state_wins()
    {
        var snapshot = Snapshot(("Photos", PathQuarantineStateFactory.CreateResyncInProgress()), ("Photos/Beach.jpg", PathQuarantineStateFactory.CreateConflicted()));

        snapshot.StateOf(SyncPaths.Of("Photos/Beach.jpg")).ShouldBe(PathQuarantineStateFactory.CreateConflicted());
    }

    [Fact]
    public void when_the_path_is_the_default_value_then_it_is_skipped()
    {
        var snapshot = QuarantineSnapshotFactory.CreateEmpty();

        snapshot.StateOf(default).ShouldBe(PathQuarantineStateFactory.CreateSkipped());
    }

    [Fact]
    public void when_no_entries_are_given_then_the_snapshot_is_empty()
    {
        var snapshot = QuarantineSnapshotFactory.Create(null!);

        snapshot.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(".obsidian/workspace.json")]
    [InlineData("Notes/.obsidian/workspace.json")]
    [InlineData("Documents/Report.docx.3f2b8c1e-5d43-4a7e-9e61-0c8a7b6d5e4f.download")]
    [InlineData("Documents/Report.docx.3F2B8C1E-5D43-4A7E-9E61-0C8A7B6D5E4F.DOWNLOAD")]
    [InlineData("Documents/Report.docx.3f2b8c1e5d434a7e9e610c8a7b6d5e4f.download")]
    public void when_the_skip_filter_matches_a_path_then_it_is_skipped(string value)
    {
        var snapshot = QuarantineSnapshotFactory.CreateEmpty();

        snapshot.StateOf(SyncPaths.Of(value)).ShouldBe(PathQuarantineStateFactory.CreateSkipped());
    }

    [Theory]
    [InlineData("Documents/notes.download")]
    [InlineData("Documents/budget.tmp")]
    [InlineData("Documents/movie.partial")]
    [InlineData("Documents/Report.docx.not-a-guid.download")]
    [InlineData("Documents/v1.2/Release notes.txt")]
    [InlineData(".gitignore")]
    [InlineData("Projects/App/.env")]
    [InlineData("Documents/.hidden-notes.txt")]
    public void when_a_user_file_only_looks_temporary_or_is_a_dot_file_then_it_is_not_skipped(string value)
    {
        var snapshot = QuarantineSnapshotFactory.CreateEmpty();

        snapshot.StateOf(SyncPaths.Of(value)).ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public void when_a_stored_path_is_also_skipped_then_skipped_wins()
    {
        var snapshot = Snapshot((".obsidian/workspace.json", PathQuarantineStateFactory.CreateConflicted()));

        snapshot.StateOf(SyncPaths.Of(".obsidian/workspace.json")).ShouldBe(PathQuarantineStateFactory.CreateSkipped());
    }

    [Fact]
    public void when_only_clear_allows_a_sync_action_then_every_other_state_blocks_it()
    {
        var states = new[] { PathQuarantineStateFactory.CreateClear(), PathQuarantineStateFactory.CreateConflicted(), PathQuarantineStateFactory.CreateHeldByGuard(), PathQuarantineStateFactory.CreateSkipped(), PathQuarantineStateFactory.CreateResyncInProgress() };

        var allowed = states.Where(state => state.AllowsSyncAction()).ToList();

        allowed.ShouldBe([PathQuarantineStateFactory.CreateClear()]);
    }

    [Fact]
    public void when_paths_with_any_mix_of_states_are_planned_then_no_non_clear_path_is_allowed_a_sync_action()
    {
        var random = new Random(1459);
        var stored = new[] { PathQuarantineStateFactory.CreateConflicted(), PathQuarantineStateFactory.CreateHeldByGuard(), PathQuarantineStateFactory.CreateResyncInProgress(), PathQuarantineStateFactory.CreateClear() };
        var folders = new[] { "Documents", "documents", ".config", "Photos/2024", "PHOTOS/2024" };
        var candidates = Enumerable.Range(0, 500).Select(index => SyncPaths.Of($"{folders[random.Next(folders.Length)]}/file{random.Next(60)}.txt")).ToList();
        var snapshot = QuarantineSnapshotFactory.Create(candidates.Where(_ => random.Next(2) == 0).Select(path => KeyValuePair.Create(path, stored[random.Next(stored.Length)])));

        var planned = candidates.Where(path => snapshot.StateOf(path).AllowsSyncAction()).ToList();

        planned.ShouldAllBe(path => !SyncSkipFilter.IsSkipped(path) && (!snapshot.Entries.ContainsKey(path) || snapshot.Entries[path] is PathQuarantineState.Clear));
    }

    [Fact]
    public void when_many_passes_read_one_snapshot_at_once_then_each_sees_the_same_state()
    {
        var snapshot = Snapshot(("Documents/Report.docx", PathQuarantineStateFactory.CreateHeldByGuard()));

        var states = Enumerable.Range(0, 20).AsParallel().Select(index => snapshot.StateOf(SyncPaths.Of(index % 2 == 0 ? "Documents/Report.docx" : "DOCUMENTS/report.docx"))).ToList();

        states.ShouldAllBe(state => state is PathQuarantineState.HeldByGuard);
    }

    private static QuarantineSnapshot Snapshot(params (string Path, PathQuarantineState State)[] entries) => QuarantineSnapshotFactory.Create(entries.Select(entry => KeyValuePair.Create(SyncPaths.Of(entry.Path), entry.State)));
}
