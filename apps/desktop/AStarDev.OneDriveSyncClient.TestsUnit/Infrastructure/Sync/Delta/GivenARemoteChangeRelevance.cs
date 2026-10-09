using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Delta;

public sealed class GivenARemoteChangeRelevance
{
    private static readonly IReadOnlyList<SyncRuleEntity> Rules = [new SyncRuleEntity { AccountId = new AccountId("user-1"), RemotePath = "/A", RuleType = RuleType.Include, RemoteItemId = Option.Some("rule-root") }];

    private static ConcurrentDictionary<string, SyncedItemEntity> Synced(params string[] ids)
        => new(ids.ToDictionary(id => id, id => new SyncedItemEntity { AccountId = new AccountId("user-1"), RemoteItemId = new OneDriveItemId(id) }), StringComparer.OrdinalIgnoreCase);

    private static DeltaChange Changed(string id, string? parent) => DeltaChangeFactory.CreateChanged(id, parent is null ? Option.None<string>() : Option.Some(parent));

    [Fact]
    public void when_there_are_no_changes_then_nothing_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([], Synced("file-1"), Rules).ShouldBeFalse();

    [Fact]
    public void when_a_known_item_changed_then_it_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("file-1", "other")], Synced("file-1"), Rules).ShouldBeTrue();

    [Fact]
    public void when_a_new_item_appears_in_a_known_folder_then_it_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("new-file", "folder-1")], Synced("folder-1"), Rules).ShouldBeTrue();

    [Fact]
    public void when_a_new_item_appears_in_an_include_rule_root_then_it_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("new-file", "rule-root")], Synced(), Rules).ShouldBeTrue();

    [Fact]
    public void when_an_include_rule_root_itself_changed_then_it_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("rule-root", "somewhere")], Synced(), Rules).ShouldBeTrue();

    [Fact]
    public void when_an_unrelated_item_changed_then_it_is_not_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("stranger", "unrelated-folder")], Synced("folder-1"), Rules).ShouldBeFalse();

    [Fact]
    public void when_a_known_item_was_deleted_then_it_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([DeltaChangeFactory.CreateDeleted("file-1")], Synced("file-1"), Rules).ShouldBeTrue();

    [Fact]
    public void when_an_unknown_item_was_deleted_then_it_is_not_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([DeltaChangeFactory.CreateDeleted("stranger")], Synced("file-1"), Rules).ShouldBeFalse();

    [Fact]
    public void when_a_changed_item_has_no_parent_and_is_unknown_then_it_is_treated_as_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("mystery", null)], Synced("file-1"), Rules).ShouldBeTrue();

    [Fact]
    public void when_only_one_of_several_changes_is_relevant_then_the_set_is_relevant()
        => RemoteChangeRelevance.AnyAffectSyncedScope([Changed("stranger", "unrelated-folder"), Changed("file-1", "folder-1")], Synced("file-1"), Rules).ShouldBeTrue();
}
