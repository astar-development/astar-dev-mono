using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPassForCreatedFolders : IDisposable
{
    private readonly UploadPassFixture fixture = new();
    private readonly UploadPassFixture nextPass = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string[] Outcomes(Result<UploadPassSummary, UploadPassFailure> result) =>
        result.Match(summary => summary.Outcomes.Select(outcome => $"{outcome.GetType().Name} {outcome.Path.Value}").Order(StringComparer.Ordinal).ToArray(), failure => [failure.GetType().Name]);

    [Fact]
    public async Task when_a_folder_is_created_then_its_baseline_row_and_remote_index_row_are_recorded_together()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFolder("Docs/New").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["FolderCreated Docs/New"]);
        var (baseline, remote) = fixture.Recorder.Recorded.ShouldHaveSingleItem();
        (baseline.Path.Value, baseline.ItemId.Value, baseline.Kind is SyncBaselineKind.Folder).ShouldBe(("Docs/New", "folder-New", true));
        (remote.Id.Value, remote.Name, remote.Kind is RemoteItemKind.Folder, remote.ETag.Value, remote.LastModified).ShouldBe(("folder-New", "New", true, "fe-New", FakeFolderCreator.Created));
        remote.ParentId.Match(parent => parent.Value, () => "none").ShouldBe("remote-docs");
    }

    [Fact]
    public async Task when_a_folder_created_in_one_pass_is_deleted_in_the_next_then_it_is_removed_with_its_entity_tag_and_not_sent_to_conflicts()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFolder("Docs/New").BuildAsync(CancellationToken);
        _ = await fixture.RunAsync(CancellationToken);
        var (baseline, remote) = fixture.Recorder.Recorded.ShouldHaveSingleItem();
        nextPass.Remote.Folder("Docs", "remote-docs");
        nextPass.Remote.Add(remote);
        nextPass.Baseline.Add(baseline);
        _ = await nextPass.DeletedItem("Docs/New", "folder-New").BuildAsync(CancellationToken);

        var result = await nextPass.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RemovedRemotely Docs/New"]);
        var call = nextPass.Remover.Calls.ShouldHaveSingleItem();
        call.ItemId.Value.ShouldBe("folder-New");
        call.Precondition.ShouldBe(RemovePreconditionFactory.CreateMustMatch(new EntityTag("fe-New")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public async Task when_the_provider_returns_a_folder_without_a_version_then_only_the_baseline_row_is_written_and_no_remote_index_row_is_made_up()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Folders.Respond = name => Result.Success<RemoteFolder, ProviderError>(RemoteFolderFactory.Create(Id($"folder-{name}"), name, Id("remote-docs"), RemoteFolderChildrenFactory.CreateNoChildFolders()));
        _ = await fixture.AddedFolder("Docs/New").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["FolderCreated Docs/New"]);
        fixture.Recorder.Recorded.ShouldBeEmpty();
        fixture.Baseline.Has("Docs/New").ShouldBeTrue();
    }

    [Fact]
    public async Task when_recording_the_created_folder_fails_then_the_folder_is_reported_as_failed_and_no_baseline_row_is_written()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Recorder.Fails = true;
        _ = await fixture.AddedFolder("Docs/New").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Failed Docs/New"]);
        fixture.Baseline.Has("Docs/New").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_folder_is_linked_to_an_existing_remote_folder_then_only_the_baseline_row_is_written()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.Folder("Docs/Existing", "existing-id");
        _ = await fixture.AddedFolder("Docs/Existing").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Linked Docs/Existing"]);
        fixture.Recorder.Recorded.ShouldBeEmpty();
        fixture.Baseline.Has("Docs/Existing").ShouldBeTrue();
    }

    public void Dispose()
    {
        fixture.Dispose();
        nextPass.Dispose();
    }
}
