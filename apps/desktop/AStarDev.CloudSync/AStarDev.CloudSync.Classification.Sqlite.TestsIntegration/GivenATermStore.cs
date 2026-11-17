using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenATermStore : MigratedDatabaseTests
{
    private async Task<ClassificationCategory> AddCategoryAsync(string name) => Ok(await Database.Categories.UpsertAsync(Category(name), CancellationToken));

    [Fact]
    public async Task when_a_term_is_upserted_then_it_is_returned_and_read_back()
    {
        var category = await AddCategoryAsync("Colours");
        var term = Term("red", category.Id);

        var stored = Ok(await Database.Terms.UpsertAsync(term, CancellationToken));

        stored.ShouldBe(term);
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBe([term]);
    }

    [Fact]
    public async Task when_the_same_text_and_category_are_upserted_with_another_id_then_the_stored_term_is_returned()
    {
        var category = await AddCategoryAsync("Colours");
        var first = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));

        var second = Ok(await Database.Terms.UpsertAsync(Term("RED", category.Id), CancellationToken));

        (second, Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((first, 1));
    }

    [Fact]
    public async Task when_the_same_text_is_upserted_in_two_categories_then_two_terms_are_stored()
    {
        var colours = await AddCategoryAsync("Colours");
        var cars = await AddCategoryAsync("Cars");

        _ = Ok(await Database.Terms.UpsertAsync(Term("jaguar", colours.Id), CancellationToken));
        _ = Ok(await Database.Terms.UpsertAsync(Term("jaguar", cars.Id), CancellationToken));

        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_an_ignored_term_is_upserted_as_active_then_it_stays_ignored_and_is_not_recreated()
    {
        var category = await AddCategoryAsync("Colours");
        var first = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));
        _ = Ok(await Database.Terms.SetStatusAsync(first.Id, TermStatusFactory.CreateIgnored(), CancellationToken));

        var again = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));

        (again.Id, again.Status, Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((first.Id, TermStatusFactory.CreateIgnored(), 1));
    }

    [Fact]
    public async Task when_an_ignored_term_is_read_then_it_is_included_in_all_terms()
    {
        var category = await AddCategoryAsync("Colours");
        var term = Term("red", category.Id, TermStatusFactory.CreateIgnored());

        _ = Ok(await Database.Terms.UpsertAsync(term, CancellationToken));

        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBe([term]);
    }

    [Fact]
    public async Task when_a_term_is_upserted_in_an_unknown_category_then_not_found_is_returned_and_nothing_is_stored()
    {
        var term = Term("red", CategoryIdFactory.Create(Guid.NewGuid()));

        var failure = Failed(await Database.Terms.UpsertAsync(term, CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateNotFound());
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_term_is_read_by_id_then_it_is_returned()
    {
        var category = await AddCategoryAsync("Colours");
        var term = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));

        var found = Ok(await Database.Terms.GetByIdAsync(term.Id, CancellationToken));

        found.Match(value => value, () => throw new InvalidOperationException("The term must be found.")).ShouldBe(term);
    }

    [Fact]
    public async Task when_an_unknown_id_is_read_then_none_is_returned()
    {
        var found = Ok(await Database.Terms.GetByIdAsync(TermIdFactory.Create(Guid.NewGuid()), CancellationToken));

        found.Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_term_is_ignored_and_activated_again_then_the_status_follows_and_the_term_is_kept()
    {
        var category = await AddCategoryAsync("Colours");
        var term = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));

        var ignored = Ok(await Database.Terms.SetStatusAsync(term.Id, TermStatusFactory.CreateIgnored(), CancellationToken));
        var active = Ok(await Database.Terms.SetStatusAsync(term.Id, TermStatusFactory.CreateActive(), CancellationToken));

        (ignored.Status, active.Status, active.Id).ShouldBe((TermStatusFactory.CreateIgnored(), TermStatusFactory.CreateActive(), term.Id));
    }

    [Fact]
    public async Task when_the_status_of_an_unknown_term_is_set_then_not_found_is_returned()
    {
        var failure = Failed(await Database.Terms.SetStatusAsync(TermIdFactory.Create(Guid.NewGuid()), TermStatusFactory.CreateIgnored(), CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_the_same_term_is_upserted_by_twenty_parallel_calls_then_one_term_is_stored_and_all_calls_return_it()
    {
        var category = await AddCategoryAsync("Colours");

        var results = await InParallelAsync(20, async _ => Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken)));

        (results.Select(term => term.Id).Distinct().Count(), Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_an_ignored_term_is_upserted_by_twenty_parallel_calls_then_it_stays_ignored()
    {
        var category = await AddCategoryAsync("Colours");
        var first = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));
        _ = Ok(await Database.Terms.SetStatusAsync(first.Id, TermStatusFactory.CreateIgnored(), CancellationToken));

        var results = await InParallelAsync(20, async _ => Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken)));

        results.ShouldAllBe(term => term.Id == first.Id && term.Status == TermStatusFactory.CreateIgnored());
    }

    [Fact]
    public async Task when_a_batch_of_new_terms_is_upserted_then_they_are_stored_and_returned_in_order()
    {
        var category = await AddCategoryAsync("Colours");
        var terms = new[] { Term("red", category.Id), Term("blue", category.Id), Term("green", category.Id) };

        var stored = Ok(await Database.Terms.UpsertManyAsync(terms, CancellationToken));

        stored.ShouldBe(terms);
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBe(terms, ignoreOrder: true);
    }

    [Fact]
    public async Task when_an_empty_batch_is_upserted_then_an_empty_list_is_returned()
    {
        var stored = Ok(await Database.Terms.UpsertManyAsync([], CancellationToken));

        stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_batch_has_existing_and_new_terms_then_the_stored_terms_are_returned_in_input_order()
    {
        var category = await AddCategoryAsync("Colours");
        var red = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));
        var blue = Term("blue", category.Id);

        var stored = Ok(await Database.Terms.UpsertManyAsync([blue, Term("red", category.Id)], CancellationToken));

        stored.ShouldBe([blue, red]);
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_batch_holds_an_ignored_term_then_it_stays_ignored_and_is_returned_as_stored()
    {
        var category = await AddCategoryAsync("Colours");
        var first = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));
        _ = Ok(await Database.Terms.SetStatusAsync(first.Id, TermStatusFactory.CreateIgnored(), CancellationToken));

        var stored = Ok(await Database.Terms.UpsertManyAsync([Term("red", category.Id), Term("blue", category.Id)], CancellationToken));

        (stored[0].Id, stored[0].Status, Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((first.Id, TermStatusFactory.CreateIgnored(), 2));
    }

    [Fact]
    public async Task when_a_batch_repeats_a_term_then_one_row_is_stored_and_both_positions_return_it()
    {
        var category = await AddCategoryAsync("Colours");

        var stored = Ok(await Database.Terms.UpsertManyAsync([Term("red", category.Id), Term("blue", category.Id), Term("red", category.Id)], CancellationToken));

        (stored[0].Id, stored[2].Id, Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((stored[0].Id, stored[0].Id, 2));
    }

    [Fact]
    public async Task when_a_batch_holds_casing_and_unicode_variants_of_one_text_then_one_row_is_stored()
    {
        var category = await AddCategoryAsync("Colours");
        var variants = new[] { Term("Caf\u00e9 Noir", category.Id), Term("CAFE\u0301   NOIR", category.Id), Term("cafe\u0301 noir", category.Id) };

        var stored = Ok(await Database.Terms.UpsertManyAsync(variants, CancellationToken));

        (stored.Select(term => term.Id).Distinct().Count(), Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_a_batch_holds_one_text_in_two_categories_then_two_terms_are_stored()
    {
        var colours = await AddCategoryAsync("Colours");
        var cars = await AddCategoryAsync("Cars");

        var stored = Ok(await Database.Terms.UpsertManyAsync([Term("jaguar", colours.Id), Term("jaguar", cars.Id)], CancellationToken));

        (stored[0].Id == stored[1].Id, Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count).ShouldBe((false, 2));
    }

    [Fact]
    public async Task when_a_batch_holds_a_term_in_an_unknown_category_then_not_found_is_returned_and_nothing_is_stored()
    {
        var category = await AddCategoryAsync("Colours");

        var failure = Failed(await Database.Terms.UpsertManyAsync([Term("red", category.Id), Term("lost", CategoryIdFactory.Create(Guid.NewGuid()))], CancellationToken));

        failure.ShouldBe(ClassificationFailureFactory.CreateNotFound());
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_batch_is_larger_than_one_transaction_then_every_term_is_stored_in_order()
    {
        var category = await AddCategoryAsync("Colours");
        var terms = Enumerable.Range(0, 2500).Select(index => Term($"term{index}", category.Id)).ToArray();

        var stored = Ok(await Database.Terms.UpsertManyAsync(terms, CancellationToken));

        stored.ShouldBe(terms);
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count.ShouldBe(2500);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_a_batch_fails_and_nothing_is_stored()
    {
        var category = await AddCategoryAsync("Colours");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var outcome = await Database.Terms.UpsertManyAsync([Term("red", category.Id)], cancellation.Token).ContinueWith(task => task.IsCompletedSuccessfully, TaskScheduler.Default);

        outcome.ShouldBeFalse();
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_overlapping_batches_and_single_upserts_run_in_twenty_parallel_calls_then_no_term_is_duplicated()
    {
        var category = await AddCategoryAsync("Colours");
        var texts = Enumerable.Range(0, 50).Select(index => $"term{index}").ToArray();

        var results = await InParallelAsync(20, async index => index % 2 == 0
            ? Ok(await Database.Terms.UpsertManyAsync([.. texts.Select(text => Term(text, category.Id))], CancellationToken))
            : [Ok(await Database.Terms.UpsertAsync(Term(texts[index], category.Id), CancellationToken))]);

        var all = Ok(await Database.Terms.GetAllAsync(CancellationToken));
        all.Count.ShouldBe(50);
        all.Select(term => term.Text).Distinct().Count().ShouldBe(50);
        results.SelectMany(batch => batch).ShouldAllBe(term => all.Any(stored => stored.Id == term.Id));
    }

    [Fact]
    public async Task when_the_service_restarts_then_the_terms_and_their_status_are_still_there()
    {
        var category = await AddCategoryAsync("Colours");
        var term = Ok(await Database.Terms.UpsertAsync(Term("red", category.Id), CancellationToken));
        var ignored = Ok(await Database.Terms.SetStatusAsync(term.Id, TermStatusFactory.CreateIgnored(), CancellationToken));

        await Database.RestartAsync();

        Ok(await Database.Terms.GetAllAsync(CancellationToken)).ShouldBe([ignored]);
    }
}
