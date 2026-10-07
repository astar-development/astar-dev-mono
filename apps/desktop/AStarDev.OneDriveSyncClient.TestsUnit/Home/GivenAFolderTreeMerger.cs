using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Home;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenAFolderTreeMerger
{
    private const string ParentPath = "/Documents";
    private static readonly Option<string> ParentId = Option.Some("parent-id");

    private static Func<string, Option<FolderSyncState>> NoRules() => _ => Option.None<FolderSyncState>();

    private static DriveFolder Remote(string id, string name) => new(id, name, ParentId);

    private static IReadOnlyList<MergedFolder> Merge(IReadOnlyList<DriveFolder> remote, IReadOnlyList<string> local, FolderSyncState defaultState = FolderSyncState.Excluded, Func<string, Option<FolderSyncState>>? rules = null)
        => FolderTreeMerger.Merge(remote, local, ParentPath, ParentId, defaultState, rules ?? NoRules());

    [Fact]
    public void when_no_folders_exist_then_result_is_empty() =>
        Merge([], []).ShouldBeEmpty();

    [Fact]
    public void when_folder_exists_only_remotely_then_origin_is_remote_only_and_remote_id_is_kept()
    {
        var result = Merge([Remote("r1", "Photos")], []);

        var folder = result.ShouldHaveSingleItem();
        folder.Origin.ShouldBeOfType<FolderOrigin.RemoteOnly>();
        folder.RemoteId.ShouldBe(Option.Some("r1"));
        folder.RemotePath.ShouldBe("/Documents/Photos");
    }

    [Fact]
    public void when_folder_exists_only_locally_then_origin_is_local_only_and_remote_id_is_none()
    {
        var result = Merge([], ["Drafts"]);

        var folder = result.ShouldHaveSingleItem();
        folder.Origin.ShouldBeOfType<FolderOrigin.LocalOnly>();
        folder.RemoteId.ShouldBe(Option.None<string>());
        folder.RemotePath.ShouldBe("/Documents/Drafts");
        folder.ParentId.ShouldBe(ParentId);
    }

    [Fact]
    public void when_folder_exists_on_both_sides_then_origin_is_both_and_remote_id_is_kept()
    {
        var result = Merge([Remote("r1", "Photos")], ["Photos"]);

        var folder = result.ShouldHaveSingleItem();
        folder.Origin.ShouldBeOfType<FolderOrigin.Both>();
        folder.RemoteId.ShouldBe(Option.Some("r1"));
    }

    [Fact]
    public void when_names_differ_only_by_case_then_they_are_treated_as_the_same_folder_and_remote_name_wins()
    {
        var result = Merge([Remote("r1", "Photos")], ["photos"]);

        var folder = result.ShouldHaveSingleItem();
        folder.Name.ShouldBe("Photos");
        folder.Origin.ShouldBeOfType<FolderOrigin.Both>();
    }

    [Fact]
    public void when_local_names_differ_only_by_case_then_one_local_only_folder_is_returned()
    {
        var result = Merge([], ["Drafts", "drafts"]);

        result.ShouldHaveSingleItem().Origin.ShouldBeOfType<FolderOrigin.LocalOnly>();
    }

    [Fact]
    public void when_folder_is_local_only_and_has_no_rule_then_it_is_included_by_default()
    {
        var result = Merge([], ["Drafts"], FolderSyncState.Excluded);

        result.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_folder_is_remote_only_and_has_no_rule_then_it_takes_the_default_state()
    {
        var result = Merge([Remote("r1", "Photos")], [], FolderSyncState.Excluded);

        result.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_folder_is_on_both_sides_and_has_no_rule_then_it_takes_the_default_state()
    {
        var result = Merge([Remote("r1", "Photos")], ["Photos"], FolderSyncState.Included);

        result.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_a_rule_exists_for_a_local_only_folder_then_the_rule_wins()
    {
        var result = Merge([], ["Drafts"], rules: path => path == "/Documents/Drafts" ? Option.Some(FolderSyncState.Excluded) : Option.None<FolderSyncState>());

        result.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_a_rule_exists_for_a_remote_folder_then_the_rule_wins()
    {
        var result = Merge([Remote("r1", "Photos")], [], FolderSyncState.Excluded, path => path == "/Documents/Photos" ? Option.Some(FolderSyncState.Included) : Option.None<FolderSyncState>());

        result.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_parent_path_is_empty_then_remote_paths_start_at_the_root()
    {
        var result = FolderTreeMerger.Merge([], ["Drafts"], string.Empty, Option.None<string>(), FolderSyncState.Excluded, NoRules());

        result.ShouldHaveSingleItem().RemotePath.ShouldBe("/Drafts");
    }

    [Fact]
    public void when_folders_come_from_both_sides_then_result_is_ordered_by_name_ignoring_case()
    {
        var result = Merge([Remote("r1", "zeta"), Remote("r2", "Alpha")], ["middle"]);

        result.Select(folder => folder.Name).ShouldBe(["Alpha", "middle", "zeta"]);
    }
}
