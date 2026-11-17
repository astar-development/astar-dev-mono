using AStarDev.CloudSync.Accounts;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenALargeLinkBatchOverSqlite : MigratedDatabaseTests
{
    private const int MoreFilesThanSqliteParameters = 33_000;

    private readonly AccountId account = NewAccount();

    private async Task<TermId> AddTermAsync(string text)
    {
        var stored = Ok(await Database.Categories.UpsertAsync(Category("Things"), CancellationToken));

        return Ok(await Database.Terms.UpsertAsync(Term(text, stored.Id), CancellationToken)).Id;
    }

    private async Task<List<TermId>> TermsOfAsync(string path) => [.. Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken)).OrderBy(term => term.Value)];

    private static List<TermId> Sorted(params TermId[] terms) => [.. terms.OrderBy(term => term.Value)];

    [Fact]
    public async Task when_a_batch_holds_more_files_than_sqlite_allows_parameters_then_every_file_gets_its_term()
    {
        var red = await AddTermAsync("red");
        var paths = Enumerable.Range(0, MoreFilesThanSqliteParameters).Select(index => $"Photos/file{index}.jpg").ToList();
        foreach (var chunk in paths.Chunk(1_000))
            await InBaselineAsync(account, chunk);

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([.. paths.Select(path => FileTermLinksFactory.Create(account, Path(path), [red]))], CancellationToken));

        var counts = Ok(await Database.Links.GetFileCountsAsync(CancellationToken));
        counts[red].ShouldBe(MoreFilesThanSqliteParameters);
    }

    [Fact]
    public async Task when_a_batch_names_the_same_path_twice_in_another_case_then_the_last_update_wins()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        await InBaselineAsync(account, "Photos/Holiday.JPG");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("Photos/Holiday.JPG"), [red]), FileTermLinksFactory.Create(account, Path("photos/holiday.jpg"), [car])], CancellationToken));

        (await TermsOfAsync("PHOTOS/HOLIDAY.JPG")).ShouldBe([car]);
    }

    [Fact]
    public async Task when_a_batch_names_the_same_path_twice_and_the_last_has_no_terms_then_the_file_has_no_automatic_links()
    {
        var red = await AddTermAsync("red");
        await InBaselineAsync(account, "a.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [red]), FileTermLinksFactory.Create(account, Path("a.jpg"), [])], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_path_is_written_then_it_is_read_back_as_the_last_update_spelled_it()
    {
        var red = await AddTermAsync("red");
        await InBaselineAsync(account, "Photos/Holiday.JPG");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("Photos/Holiday.JPG"), [red]), FileTermLinksFactory.Create(account, Path("photos/HOLIDAY.jpg"), [red])], CancellationToken));

        var files = Ok(await Database.Links.GetFilesForTermAsync(red, CancellationToken));
        files.Select(file => (file.AccountId, file.Path.Value)).ShouldBe([(account, "photos/HOLIDAY.jpg")]);
    }

    [Fact]
    public async Task when_a_batch_replaces_files_with_user_links_then_every_user_link_stays_a_user_link()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        await InBaselineAsync(account, "a.jpg", "b.jpg", "c.jpg");
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("B.JPG"), car, CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [red, car]), FileTermLinksFactory.Create(account, Path("b.jpg"), [car, red]), FileTermLinksFactory.Create(account, Path("c.jpg"), [red])], CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), []), FileTermLinksFactory.Create(account, Path("b.jpg"), []), FileTermLinksFactory.Create(account, Path("c.jpg"), [])], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
        (await TermsOfAsync("b.jpg")).ShouldBe([car]);
        (await TermsOfAsync("c.jpg")).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_batch_holds_two_accounts_then_each_gets_only_its_own_links()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        var other = NewAccount();
        await InBaselineAsync(account, "a.jpg");
        await InBaselineAsync(other, "a.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [red]), FileTermLinksFactory.Create(other, Path("A.JPG"), [car])], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
        Ok(await Database.Links.GetTermsForFileAsync(other, Path("a.jpg"), CancellationToken)).ShouldBe([car]);
    }

    [Fact]
    public async Task when_a_batch_mixes_a_user_linked_term_with_new_terms_then_both_are_kept_once()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        await InBaselineAsync(account, "a.jpg");
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [red, car, car])], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe(Sorted(red, car));
    }

    [Fact]
    public async Task when_the_second_account_of_a_batch_names_an_unknown_term_then_the_first_account_is_untouched()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        var other = NewAccount();
        await InBaselineAsync(account, "a.jpg");
        await InBaselineAsync(other, "b.jpg");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [red])], CancellationToken));

        var failure = Failed(await Database.Links.ReplaceAutoLinksAsync([FileTermLinksFactory.Create(account, Path("a.jpg"), [car]), FileTermLinksFactory.Create(other, Path("b.jpg"), [TermIdFactory.Create(Guid.NewGuid())])], CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateUnavailable());
        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }
}
