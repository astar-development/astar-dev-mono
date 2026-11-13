using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPassForRemovals : IDisposable
{
    private readonly UploadPassFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string[] Outcomes(Result<UploadPassSummary, UploadPassFailure> result) =>
        result.Match(summary => summary.Outcomes.Select(outcome => $"{outcome.GetType().Name} {outcome.Path.Value}").Order(StringComparer.Ordinal).ToArray(), failure => [failure.GetType().Name]);

    [Fact]
    public async Task when_a_file_was_deleted_locally_and_is_unchanged_remotely_then_it_is_removed_with_the_content_tag_and_the_baseline_row_goes()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1");
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").DeletedItem("Docs/a.txt", "r1").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RemovedRemotely Docs/a.txt"]);
        var call = fixture.Remover.Calls.ShouldHaveSingleItem();
        call.ItemId.ShouldBe(Id("r1"));
        call.Precondition.ShouldBe(RemovePreconditionFactory.CreateMustMatchContent(new ContentTag("c1")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_for_a_delete_then_it_goes_to_conflicts_and_the_baseline_row_stays()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1");
        fixture.Remover.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").DeletedItem("Docs/a.txt", "r1").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["SentToConflicts Docs/a.txt"]);
        fixture.Baseline.Has("Docs/a.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_remote_file_is_already_gone_then_only_the_baseline_row_is_removed_and_the_remover_is_not_called()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").DeletedItem("Docs/a.txt", "r1").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RemovedRemotely Docs/a.txt"]);
        fixture.Remover.Calls.ShouldBeEmpty();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_remote_copy_changed_since_the_baseline_then_the_delete_goes_to_conflicts_without_calling_the_remover()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c2");
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").DeletedItem("Docs/a.txt", "r1").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["SentToConflicts Docs/a.txt"]);
        fixture.Remover.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_folder_with_files_was_deleted_locally_then_the_files_go_first_then_the_folder_and_the_baseline_is_emptied()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.Folder("Docs/Old", "old-id");
        fixture.Remote.File("Docs/Old/x.txt", "r2", "c2");
        _ = await fixture.BaselineFolder("Docs/Old", "old-id").BaselineFile("Docs/Old/x.txt", "r2", "c2")
            .DeletedItem("Docs/Old", "old-id").DeletedItem("Docs/Old/x.txt", "r2").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RemovedRemotely Docs/Old", "RemovedRemotely Docs/Old/x.txt"]);
        fixture.Remover.Calls.Select(call => call.ItemId.Value).ShouldBe(["r2", "old-id"]);
        fixture.Baseline.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_remote_file_appeared_inside_a_folder_deleted_locally_then_the_folder_is_not_removed()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.Folder("Docs/Old", "old-id");
        fixture.Remote.File("Docs/Old/new-remote.txt", "r9", "c9");
        _ = await fixture.BaselineFolder("Docs/Old", "old-id").DeletedItem("Docs/Old", "old-id").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["SentToConflicts Docs/Old"]);
        fixture.Remover.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_file_is_renamed_locally_then_the_new_copy_uploads_before_the_old_one_is_removed()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/old.txt", "r1", "c1");
        var uploadsWhenRemoving = -1;
        fixture.Remover.OnRemove = () => uploadsWhenRemoving = fixture.Uploader.Calls.Count;
        _ = await fixture.BaselineFile("Docs/old.txt", "r1", "c1").DeletedItem("Docs/old.txt", "r1").AddedFile("Docs/new.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RemovedRemotely Docs/old.txt", "Uploaded Docs/new.txt"]);
        uploadsWhenRemoving.ShouldBe(1);
    }

    public void Dispose() => fixture.Dispose();
}
