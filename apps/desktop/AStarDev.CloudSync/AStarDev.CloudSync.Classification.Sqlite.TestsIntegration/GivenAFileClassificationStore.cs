using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAFileClassificationStore : MigratedDatabaseTests
{
    private readonly AccountId account = NewAccount();

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await InBaselineAsync(account, "a.jpg", "b.jpg", "c.jpg", "Photos/Holiday.JPG", "Ärger/a.jpg");
    }

    private async Task<TermId> AddTermAsync(string text, string category = "Things")
    {
        var stored = Ok(await Database.Categories.UpsertAsync(Category(category), CancellationToken));

        return Ok(await Database.Terms.UpsertAsync(Term(text, stored.Id), CancellationToken)).Id;
    }

    private async Task<List<TermId>> TermsOfAsync(string path) => [.. Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken)).OrderBy(term => term.Value)];

    private static List<TermId> Sorted(params TermId[] terms) => [.. terms.OrderBy(term => term.Value)];

    [Fact]
    public async Task when_a_file_has_no_links_then_no_terms_are_read()
    {
        (await TermsOfAsync("a.jpg")).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_auto_links_are_replaced_then_the_terms_are_read_back()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red, car], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe(Sorted(red, car));
    }

    [Fact]
    public async Task when_auto_links_are_replaced_again_then_the_old_automatic_links_are_gone()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [car], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([car]);
    }

    [Fact]
    public async Task when_the_same_term_is_given_twice_then_one_link_is_stored()
    {
        var red = await AddTermAsync("red");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red, red], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_auto_links_are_replaced_then_a_user_link_stays()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [car], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe(Sorted(red, car));
    }

    [Fact]
    public async Task when_auto_links_are_replaced_with_a_term_the_user_assigned_then_the_link_stays_a_user_link()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_a_user_link_is_assigned_over_an_automatic_link_then_it_survives_the_next_replace()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [], CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_a_user_link_is_assigned_to_an_unknown_term_then_not_found_is_returned()
    {
        var failure = Failed(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), TermIdFactory.Create(Guid.NewGuid()), CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_a_user_link_is_assigned_to_a_path_not_in_the_baseline_then_file_not_found_is_returned_and_no_link_is_stored()
    {
        var red = await AddTermAsync("red");

        var failure = Failed(await Database.Links.AssignUserLinkAsync(account, Path("missing.jpg"), red, CancellationToken));

        (failure, (await TermsOfAsync("missing.jpg")).Count).ShouldBe((ClassificationFailureFactory.CreateFileNotFound(), 0));
    }

    [Fact]
    public async Task when_a_user_link_is_assigned_to_an_unknown_term_and_a_path_not_in_the_baseline_then_not_found_is_returned()
    {
        var failure = Failed(await Database.Links.AssignUserLinkAsync(account, Path("missing.jpg"), TermIdFactory.Create(Guid.NewGuid()), CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_a_replace_names_an_unknown_term_then_it_fails_and_the_existing_links_are_untouched()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));

        var failure = Failed(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [TermIdFactory.Create(Guid.NewGuid())], CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateUnavailable());
        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_a_path_is_given_in_another_case_then_it_is_the_same_file()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("Photos/Holiday.JPG"), [red], CancellationToken));

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("photos/holiday.jpg"), [car], CancellationToken));

        (await TermsOfAsync("PHOTOS/HOLIDAY.JPG")).ShouldBe([car]);
    }

    [Fact]
    public async Task when_a_path_differs_only_by_a_non_ascii_letter_case_then_it_is_the_same_file()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("Ärger/a.jpg"), red, CancellationToken));

        (await TermsOfAsync("ärger/A.JPG")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_the_same_file_is_in_two_accounts_then_the_links_are_kept_apart()
    {
        var red = await AddTermAsync("red");
        var other = NewAccount();
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));

        var otherTerms = Ok(await Database.Links.GetTermsForFileAsync(other, Path("a.jpg"), CancellationToken));

        otherTerms.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_batch_is_replaced_then_every_file_gets_its_terms()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        var updates = new[] { FileTermLinksFactory.Create(account, Path("a.jpg"), [red]), FileTermLinksFactory.Create(account, Path("b.jpg"), [car]), FileTermLinksFactory.Create(account, Path("c.jpg"), []) };

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(updates, CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
        (await TermsOfAsync("b.jpg")).ShouldBe([car]);
        (await TermsOfAsync("c.jpg")).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_batch_names_an_unknown_term_then_it_fails_and_no_file_changes()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));
        var updates = new[] { FileTermLinksFactory.Create(account, Path("a.jpg"), []), FileTermLinksFactory.Create(account, Path("b.jpg"), [TermIdFactory.Create(Guid.NewGuid())]) };

        var failure = Failed(await Database.Links.ReplaceAutoLinksAsync(updates, CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateUnavailable());
        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_files_are_read_for_a_term_then_every_linked_file_is_returned_across_accounts()
    {
        var red = await AddTermAsync("red");
        var other = NewAccount();
        await InBaselineAsync(other, "b.jpg");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));
        _ = Ok(await Database.Links.AssignUserLinkAsync(other, Path("b.jpg"), red, CancellationToken));

        var files = Ok(await Database.Links.GetFilesForTermAsync(red, CancellationToken));

        files.Select(file => (file.AccountId, file.Path.Value)).ShouldBe([(account, "a.jpg"), (other, "b.jpg")], ignoreOrder: true);
    }

    [Fact]
    public async Task when_a_file_is_removed_then_all_its_links_go_including_user_links()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [car], CancellationToken));
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken));
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("b.jpg"), red, CancellationToken));

        _ = Ok(await Database.Links.RemoveAsync(account, Path("A.JPG"), CancellationToken));

        (await TermsOfAsync("a.jpg")).ShouldBeEmpty();
        (await TermsOfAsync("b.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_a_missing_file_is_removed_then_the_call_succeeds()
    {
        var removed = await Database.Links.RemoveAsync(account, Path("missing.jpg"), CancellationToken);

        Ok(removed).ShouldBe(Unit.Instance);
    }

    [Fact]
    public async Task when_file_counts_are_read_then_each_term_has_its_number_of_files_and_unused_terms_are_absent()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        _ = await AddTermAsync("unused");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red, car], CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("b.jpg"), [red], CancellationToken));

        var counts = Ok(await Database.Links.GetFileCountsAsync(CancellationToken));

        (counts.Count, counts[red], counts[car]).ShouldBe((2, 2, 1));
    }

    [Fact]
    public async Task when_the_same_file_is_replaced_by_twenty_parallel_calls_then_one_consistent_set_of_links_remains()
    {
        var terms = new[] { await AddTermAsync("red"), await AddTermAsync("car"), await AddTermAsync("blue") };

        var results = await InParallelAsync(20, async index => await Database.Links.ReplaceAutoLinksAsync(account, Path(index % 2 == 0 ? "A.jpg" : "a.JPG"), [terms[index % terms.Length]], CancellationToken));

        (results.All(result => result.Match(_ => true, _ => false)), (await TermsOfAsync("a.jpg")).Count).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_user_links_are_assigned_by_twenty_parallel_calls_then_each_link_is_stored_once()
    {
        var red = await AddTermAsync("red");

        _ = await InParallelAsync(20, async _ => Ok(await Database.Links.AssignUserLinkAsync(account, Path("a.jpg"), red, CancellationToken)));

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }

    [Fact]
    public async Task when_the_service_restarts_then_the_links_are_still_there()
    {
        var red = await AddTermAsync("red");
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("a.jpg"), [red], CancellationToken));

        await Database.RestartAsync();

        (await TermsOfAsync("a.jpg")).ShouldBe([red]);
    }
}
