using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenALinkWriteFromBeforeARootChangeOverSqlite : MigratedDatabaseTests
{
    private const string FirstRoot = "/home/jbarden/CloudSync/OneDrive";
    private const string SecondRoot = "/home/jbarden/CloudSync/Elsewhere";
    private const string KeptRoot = "/home/jbarden/CloudSync/Other";

    private readonly AccountId changedAccount = NewAccount();
    private readonly AccountId keptAccount = NewAccount();

    private async Task<TermId> AddTermAsync(string text)
    {
        var stored = Ok(await Database.Categories.UpsertAsync(Category($"Category {text}"), CancellationToken));

        return Ok(await Database.Terms.UpsertAsync(Term(text, stored.Id), CancellationToken)).Id;
    }

    private async Task<IReadOnlyList<TermId>> TermsOfAsync(AccountId account, string path) => Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken));

    private async Task<string> SetRootAsync(AccountId account, string path) =>
        (await Database.SyncRootWriter.SetAsync(account, Database.Folder(path), CancellationToken)).Match(root => root.Directory.FullName, rejection => rejection.GetType().Name);

    private async Task<TermId> ChangeRootAfterOldRootSyncAsync(params string[] oldPaths)
    {
        _ = await SetRootAsync(changedAccount, FirstRoot);
        await InBaselineAsync(changedAccount, oldPaths);
        var red = await AddTermAsync("red");
        _ = await SetRootAsync(changedAccount, SecondRoot);

        return red;
    }

    [Fact]
    public async Task when_a_batch_streamed_before_the_root_change_is_written_after_it_then_the_old_root_path_gets_no_links()
    {
        var red = await ChangeRootAfterOldRootSyncAsync("old.jpg");

        var written = await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(changedAccount, Path("old.jpg"), [red])], CancellationToken);

        (written.Match(_ => true, _ => false), (await TermsOfAsync(changedAccount, "old.jpg")).Count).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_a_batch_mixes_old_root_and_new_root_paths_then_only_the_new_root_paths_get_links()
    {
        var red = await ChangeRootAfterOldRootSyncAsync("old.jpg");
        await InBaselineAsync(changedAccount, "new.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(changedAccount, Path("old.jpg"), [red]), FileTermLinksFactory.Create(changedAccount, Path("new.jpg"), [red])], CancellationToken));

        (await TermsOfAsync(changedAccount, "old.jpg")).ShouldBeEmpty();
        (await TermsOfAsync(changedAccount, "new.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_a_batch_holds_another_accounts_current_paths_then_that_account_still_gets_its_links()
    {
        _ = await SetRootAsync(keptAccount, KeptRoot);
        await InBaselineAsync(keptAccount, "old.jpg");
        var red = await ChangeRootAfterOldRootSyncAsync("old.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(changedAccount, Path("old.jpg"), [red]), FileTermLinksFactory.Create(keptAccount, Path("old.jpg"), [red])], CancellationToken));

        (await TermsOfAsync(changedAccount, "old.jpg")).ShouldBeEmpty();
        (await TermsOfAsync(keptAccount, "old.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_the_path_is_given_in_another_case_than_the_baseline_then_the_links_are_written()
    {
        var red = await ChangeRootAfterOldRootSyncAsync("old.jpg");
        await InBaselineAsync(changedAccount, "Photos/Ärger.JPG");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(changedAccount, Path("photos/ärger.jpg"), [red], CancellationToken));

        (await TermsOfAsync(changedAccount, "PHOTOS/ÄRGER.JPG")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_an_old_root_path_is_given_in_another_case_then_it_still_gets_no_links()
    {
        var red = await ChangeRootAfterOldRootSyncAsync("Photos/Old.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(changedAccount, Path("photos/OLD.JPG"), [red], CancellationToken));

        (await TermsOfAsync(changedAccount, "Photos/Old.jpg")).Count.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_user_link_is_assigned_to_an_old_root_path_then_file_not_found_is_returned_and_no_link_is_stored()
    {
        var red = await ChangeRootAfterOldRootSyncAsync("old.jpg");

        var failure = Failed(await Database.Links.AssignUserLinkAsync(changedAccount, Path("old.jpg"), red, CancellationToken));

        (failure, (await TermsOfAsync(changedAccount, "old.jpg")).Count).ShouldBe((ClassificationFailureFactory.CreateFileNotFound(), 0));
    }

    [Fact]
    public async Task when_twenty_parallel_link_writes_race_a_root_change_then_no_old_root_links_are_left()
    {
        _ = await SetRootAsync(changedAccount, FirstRoot);
        await InBaselineAsync(changedAccount, "old.jpg");
        var red = await AddTermAsync("red");

        var outcomes = await InParallelAsync(21, async index => index == 10
            ? await SetRootAsync(changedAccount, SecondRoot)
            : (await Database.Links.ReplaceAutoLinksAsync(changedAccount, Path(index % 2 == 0 ? "old.jpg" : "OLD.jpg"), [red], CancellationToken)).Match(_ => "written", failure => failure.GetType().Name));

        (outcomes.Count(outcome => outcome == SecondRoot), outcomes.Count(outcome => outcome == "written"), (await TermsOfAsync(changedAccount, "old.jpg")).Count).ShouldBe((1, 20, 0));
    }
}
