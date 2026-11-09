using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAFolderTreeSource
{
    private readonly ScriptedRemoteBrowser remote = new();
    private readonly ScriptedLocalBrowser local = new();
    private readonly ScriptedRuleReader rules = new();
    private Result<IRemoteFolderBrowser, ProviderError>? capability;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_level_has_folders_on_both_sides_then_they_are_merged_marked_by_origin_and_sorted_ignoring_case()
    {
        local.Listing = Local(LocalFolder("Photos", false), LocalFolder("Docs", true));
        remote.Listing = Remote(RemoteFolder("r-docs", "docs", true), RemoteFolder("r-music", "Music", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        Nodes(result).Select(node => (node.Name, node.Origin)).ShouldBe([("Docs", FolderOriginFactory.CreateBoth()), ("Music", FolderOriginFactory.CreateRemoteOnly()), ("Photos", FolderOriginFactory.CreateLocalOnly())]);
    }

    [Fact]
    public async Task when_a_folder_is_on_both_sides_under_different_casing_then_it_is_one_node_named_and_pathed_as_on_disk()
    {
        local.Listing = Local(LocalFolder("Docs", false));
        remote.Listing = Remote(RemoteFolder("r-docs", "DOCS", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        var node = Nodes(result).ShouldHaveSingleItem();
        (node.Name, node.Path.Value, node.RemoteId.Match(id => id.Value, () => "none")).ShouldBe(("Docs", "Docs", "r-docs"));
    }

    [Fact]
    public async Task when_the_children_are_listed_below_a_folder_then_the_paths_include_the_parent()
    {
        local.Listing = Local(LocalFolder("Sub", false, "Docs"));
        remote.Listing = Remote(RemoteFolder("r-sub", "Sub", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateFolder(Path("Docs"), ItemId("r-docs")));

        Nodes(result).Select(node => node.Path.Value).ShouldBe(["Docs/Sub"]);
    }

    [Theory]
    [InlineData(true, false, "May")]
    [InlineData(false, true, "May")]
    [InlineData(true, true, "May")]
    [InlineData(false, false, "No")]
    public async Task when_either_side_reports_child_folders_then_the_node_may_have_children(bool localHasChildren, bool remoteHasChildren, string expected)
    {
        local.Listing = Local(LocalFolder("Docs", localHasChildren));
        remote.Listing = Remote(RemoteFolder("r-docs", "Docs", remoteHasChildren));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        Nodes(result).Single().Children.ShouldBe(expected == "May" ? FolderChildrenFactory.CreateMayHaveChildFolders() : FolderChildrenFactory.CreateNoChildFolders());
    }

    [Fact]
    public async Task when_two_local_folders_differ_only_by_case_then_they_are_one_node_with_a_case_clash()
    {
        local.Listing = Local(LocalFolder("Docs", false), LocalFolder("docs", true));
        remote.Listing = Remote(RemoteFolder("r-docs", "Docs", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        var node = Nodes(result).ShouldHaveSingleItem();
        (node.NameClash, node.Origin, node.Children).ShouldBe((FolderNameClashFactory.CreateCaseClash(), FolderOriginFactory.CreateBoth(), FolderChildrenFactory.CreateMayHaveChildFolders()));
    }

    [Fact]
    public async Task when_local_names_are_unique_then_there_is_no_case_clash()
    {
        local.Listing = Local(LocalFolder("Docs", false), LocalFolder("Photos", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        Nodes(result).ShouldAllBe(node => node.NameClash is FolderNameClash.Unique);
    }

    [Fact]
    public async Task when_rules_exist_then_each_node_shows_the_state_from_the_stored_rules()
    {
        local.Listing = Local(LocalFolder("Docs", false), LocalFolder("Photos", false), LocalFolder("Misc", false));
        rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([Include("Docs", SyncDirectionFactory.CreateUploadOnly()), Exclude("Photos")]);

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        Nodes(result).Select(node => (node.Name, node.Selection)).ShouldBe([
            ("Docs", SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateUploadOnly())),
            ("Misc", SelectionStateFactory.CreateUncovered()),
            ("Photos", SelectionStateFactory.CreateExcluded())]);
    }

    [Fact]
    public async Task when_a_rule_sits_deep_below_an_unloaded_folder_then_the_nodes_above_it_show_partial_at_every_depth()
    {
        local.Listing = Local(LocalFolder("A", true));
        rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([Include("A"), Exclude("A/B/C")]);

        var root = Nodes(await ListAsync(FolderTreePositionFactory.CreateRoot())).Single();
        local.Listing = Local(LocalFolder("B", true, "A"));
        var below = Nodes(await ListAsync(FolderTreePositionFactory.CreateFolder(Path("A"), Option.None<RemoteItemId>()))).Single();

        (root.Selection, below.Selection).ShouldBe((SelectionStateFactory.CreatePartiallyIncluded(), SelectionStateFactory.CreatePartiallyIncluded()));
    }

    [Fact]
    public async Task when_the_same_rules_are_read_again_then_the_same_states_show()
    {
        local.Listing = Local(LocalFolder("Docs", false));
        rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([Include("Docs")]);

        var first = Nodes(await ListAsync(FolderTreePositionFactory.CreateRoot())).Single().Selection;
        var second = Nodes(await ListAsync(FolderTreePositionFactory.CreateRoot())).Single().Selection;

        second.ShouldBe(first);
    }

    [Fact]
    public async Task when_a_folder_is_listed_then_the_cloud_is_asked_for_its_remote_id_and_the_disk_for_its_path_once()
    {
        _ = await ListAsync(FolderTreePositionFactory.CreateFolder(Path("Docs/Work"), ItemId("r-work")));

        (remote.Parents.Count, remote.Parents[0].Match(id => id.Value, () => "none"), local.Parents.Count, local.Parents[0].Value).ShouldBe((1, "r-work", 1, "Docs/Work"));
    }

    [Fact]
    public async Task when_the_root_is_listed_then_the_cloud_is_asked_for_its_root()
    {
        _ = await ListAsync(FolderTreePositionFactory.CreateRoot());

        (remote.Parents.Single().Match(_ => "id", () => "none"), local.Parents.Single()).ShouldBe(("none", LocalRelativePathFactory.Root));
    }

    [Fact]
    public async Task when_a_folder_has_no_cloud_folder_then_the_cloud_is_not_asked_and_the_level_lists_from_disk()
    {
        local.Listing = Local(LocalFolder("New", false, "OnlyHere"));

        var result = await ListAsync(FolderTreePositionFactory.CreateFolder(Path("OnlyHere"), Option.None<RemoteItemId>()));

        (remote.Parents.Count, Nodes(result).Select(node => node.Origin).ToList()).ShouldBe((0, [FolderOriginFactory.CreateLocalOnly()]));
    }

    [Fact]
    public async Task when_a_folder_is_missing_on_disk_then_its_cloud_children_are_listed_as_cloud_only()
    {
        local.Listing = Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(LocalFolderErrorFactory.CreateNotFound());
        remote.Listing = Remote(RemoteFolder("r-sub", "Sub", false));

        var result = await ListAsync(FolderTreePositionFactory.CreateFolder(Path("Docs"), ItemId("r-docs")));

        Nodes(result).Select(node => node.Origin).ToList().ShouldBe([FolderOriginFactory.CreateRemoteOnly()]);
    }

    [Fact]
    public async Task when_the_cloud_listing_fails_then_the_level_fails_naming_the_cloud_and_is_not_listed_as_empty()
    {
        local.Listing = Local(LocalFolder("Docs", false));
        remote.Listing = Result.Failure<IReadOnlyList<RemoteFolder>, ProviderError>(ProviderErrorFactory.CreateTransient());

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        result.Match(_ => (object)"listed", failure => failure).ShouldBe(FolderTreeFailureFactory.CreateRemoteFailed(ProviderErrorFactory.CreateTransient()));
    }

    [Fact]
    public async Task when_the_disk_listing_fails_then_the_level_fails_naming_the_disk()
    {
        remote.Listing = Remote(RemoteFolder("r-docs", "Docs", false));
        local.Listing = Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(LocalFolderErrorFactory.CreateAccessDenied());

        var result = await ListAsync(FolderTreePositionFactory.CreateFolder(Path("Docs"), ItemId("r-docs")));

        result.Match(_ => (object)"listed", failure => failure).ShouldBe(FolderTreeFailureFactory.CreateLocalFailed(LocalFolderErrorFactory.CreateAccessDenied()));
    }

    [Fact]
    public async Task when_the_sync_root_is_unavailable_then_the_root_level_fails_naming_the_disk()
    {
        local.Listing = Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(LocalFolderErrorFactory.CreateRootUnavailable());

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        result.Match(_ => (object)"listed", failure => failure).ShouldBe(FolderTreeFailureFactory.CreateLocalFailed(LocalFolderErrorFactory.CreateRootUnavailable()));
    }

    [Fact]
    public async Task when_both_listings_fail_then_both_errors_are_kept()
    {
        remote.Listing = Result.Failure<IReadOnlyList<RemoteFolder>, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());
        local.Listing = Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(LocalFolderErrorFactory.CreateRootUnavailable());

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        result.Match(_ => (object)"listed", failure => failure).ShouldBe(FolderTreeFailureFactory.CreateBothFailed(ProviderErrorFactory.CreateAuthenticationRequired(), LocalFolderErrorFactory.CreateRootUnavailable()));
    }

    [Fact]
    public async Task when_the_provider_has_no_folder_browser_then_the_level_fails_naming_the_cloud()
    {
        capability = Result.Failure<IRemoteFolderBrowser, ProviderError>(ProviderErrorFactory.CreateUnsupported());

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        result.Match(_ => (object)"listed", failure => failure).ShouldBe(FolderTreeFailureFactory.CreateRemoteFailed(ProviderErrorFactory.CreateUnsupported()));
    }

    [Fact]
    public async Task when_the_rules_cannot_be_read_then_the_level_fails_rather_than_showing_guessed_states()
    {
        local.Listing = Local(LocalFolder("Docs", false));
        rules.Listing = Result.Failure<IReadOnlyList<SelectionRule>, ErrorResponse>(new ErrorResponse("rules failed"));

        var result = await ListAsync(FolderTreePositionFactory.CreateRoot());

        result.Match(_ => false, failure => failure is FolderTreeFailure.RulesFailed).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_token_is_cancelled_while_listing_then_the_listing_is_cancelled()
    {
        using var cancelled = new CancellationTokenSource();
        local.Wait = async token =>
        {
            await cancelled.CancelAsync();
            token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
        };

        var listing = Source().GetChildrenAsync(Account, FolderTreePositionFactory.CreateRoot(), cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(listing);
    }

    private static Result<IReadOnlyList<LocalFolder>, LocalFolderError> Local(params LocalFolder[] folders) => Result.Success<IReadOnlyList<LocalFolder>, LocalFolderError>(folders);

    private static Result<IReadOnlyList<RemoteFolder>, ProviderError> Remote(params RemoteFolder[] folders) => Result.Success<IReadOnlyList<RemoteFolder>, ProviderError>(folders);

    private static LocalFolder LocalFolder(string name, bool hasChildren, string? parent = null) =>
        LocalFolderFactory.Create(name, Path(parent is null ? name : $"{parent}/{name}"), hasChildren ? LocalFolderChildrenFactory.CreateHasChildFolders() : LocalFolderChildrenFactory.CreateNoChildFolders());

    private static RemoteFolder RemoteFolder(string id, string name, bool mayHaveChildren) =>
        RemoteFolderFactory.Create(ItemId(id), name, ItemId("parent"), mayHaveChildren ? RemoteFolderChildrenFactory.CreateMayHaveChildFolders() : RemoteFolderChildrenFactory.CreateNoChildFolders());

    private static RemoteItemId ItemId(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    private static Option<RemoteItemId> ItemIdOption(string value) => Option.Some(ItemId(value));

    private static List<FolderTreeNode> Nodes(Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> result) => [.. result.Match(nodes => nodes, _ => [])];

    private Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> ListAsync(FolderTreePosition parent) => Source().GetChildrenAsync(Account, parent, CancellationToken);

    private FolderTreeSource Source() => new(new ScriptedCapability(capability ?? Result.Success<IRemoteFolderBrowser, ProviderError>(remote)), local, rules, new SyncSelectionEvaluator());
}
