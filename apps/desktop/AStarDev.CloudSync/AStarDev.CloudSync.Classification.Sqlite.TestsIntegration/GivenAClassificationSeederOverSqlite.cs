using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAClassificationSeederOverSqlite : MigratedDatabaseTests
{
    private static SeedTerm SeedTerm(string text, string category, SeedTermKind? kind = null) =>
        SeedTermFactory.Create(text, category, kind ?? SeedTermKindFactory.CreateWord()).Match(term => term, () => throw new InvalidOperationException("The test term must be valid."));

    private static ClassificationSeed SmallSeed(string version) =>
        ClassificationSeedFactory.Create(version, [SeedCategoryFactory.Create("Colour"), SeedCategoryFactory.Create("Image")], [SeedTerm("red", "Colour"), SeedTerm("jpg", "Image", SeedTermKindFactory.CreateExtension()), SeedTerm("holiday photos", "Image", SeedTermKindFactory.CreateFolder())]);

    private ClassificationSeeder SeederFor(IClassificationSeedSource source) =>
        new(source, Database.SeedState, Database.Categories, Database.Terms, NullLogger<ClassificationSeeder>.Instance);

    private async Task<ClassificationCategory> AddCategoryAsync(string name) => Ok(await Database.Categories.UpsertAsync(Category(name), CancellationToken));

    [Fact]
    public async Task when_the_seed_is_applied_then_categories_and_active_seed_terms_are_stored_and_the_version_recorded()
    {
        var outcome = await SeederFor(new SeedingSeedSource(SmallSeed("3"))).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        Ok(await Database.Categories.GetAllAsync(CancellationToken)).Select(category => category.Name).ShouldBe(["Colour", "Image"], ignoreOrder: true);
        var terms = Ok(await Database.Terms.GetAllAsync(CancellationToken));
        terms.Select(term => term.Text).ShouldBe(["red", "jpg", "holiday photos"], ignoreOrder: true);
        terms.ShouldAllBe(term => term.Source is TermSource.Seed && term.Status is TermStatus.Active);
        Ok(await Database.SeedState.GetAppliedVersionAsync(CancellationToken)).Match(version => version, () => string.Empty).ShouldBe("3");
    }

    [Fact]
    public async Task when_the_seed_runs_twice_then_the_second_run_changes_nothing()
    {
        var seeder = SeederFor(new SeedingSeedSource(SmallSeed("3")));
        _ = await seeder.SeedAsync(CancellationToken);
        var before = Ok(await Database.Terms.GetAllAsync(CancellationToken));

        var outcome = await seeder.SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.AlreadyCurrent>();
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Select(term => term.Id).ShouldBe(before.Select(term => term.Id), ignoreOrder: true);
    }

    [Fact]
    public async Task when_a_changed_seed_is_applied_over_the_old_one_then_terms_are_not_duplicated()
    {
        _ = await SeederFor(new SeedingSeedSource(SmallSeed("3"))).SeedAsync(CancellationToken);

        var outcome = await SeederFor(new SeedingSeedSource(SmallSeed("4"))).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_a_seed_term_was_ignored_then_a_changed_seed_neither_un_ignores_nor_recreates_it()
    {
        var seeder = SeederFor(new SeedingSeedSource(SmallSeed("3")));
        _ = await seeder.SeedAsync(CancellationToken);
        var red = Ok(await Database.Terms.GetAllAsync(CancellationToken)).Single(term => term.Text == "red");
        _ = Ok(await Database.Terms.SetStatusAsync(red.Id, TermStatusFactory.CreateIgnored(), CancellationToken));

        _ = await SeederFor(new SeedingSeedSource(SmallSeed("4"))).SeedAsync(CancellationToken);

        var after = Ok(await Database.Terms.GetAllAsync(CancellationToken)).Where(term => term.Text == "red").ShouldHaveSingleItem();
        after.Id.ShouldBe(red.Id);
        after.Status.ShouldBeOfType<TermStatus.Ignored>();
    }

    [Fact]
    public async Task when_a_user_term_exists_then_a_seed_leaves_it_alone()
    {
        var colour = await AddCategoryAsync("Colour");
        var userTerm = ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), "red", colour.Id, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()).Match(term => term, () => throw new InvalidOperationException());
        _ = Ok(await Database.Terms.UpsertAsync(userTerm, CancellationToken));
        var extra = Ok(await Database.Terms.UpsertAsync(Term("crimson", colour.Id), CancellationToken));

        _ = await SeederFor(new SeedingSeedSource(SmallSeed("3"))).SeedAsync(CancellationToken);

        var terms = Ok(await Database.Terms.GetAllAsync(CancellationToken));
        terms.Where(term => term.Text == "red").ShouldHaveSingleItem().Source.ShouldBeOfType<TermSource.User>();
        terms.Select(term => term.Id).ShouldContain(extra.Id);
    }

    [Fact]
    public async Task when_the_embedded_seed_is_applied_then_the_sqlite_stores_hold_it_and_the_second_run_is_a_no_op()
    {
        var seeder = SeederFor(EmbeddedClassificationSeedSourceFactory.CreateDefault());
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var first = await seeder.SeedAsync(CancellationToken);
        var firstElapsed = clock.Elapsed;
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var second = await seeder.SeedAsync(CancellationToken);

        first.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        second.ShouldBeOfType<ClassificationSeedOutcome.AlreadyCurrent>();
        var termCount = Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count;
        termCount.ShouldBeGreaterThan(0);
        TestContext.Current.SendDiagnosticMessage($"Seeded {termCount} terms in {firstElapsed.TotalSeconds:F2}s, {allocated / 1024 / 1024} MB allocated");
    }

    [Fact]
    public async Task when_many_terms_are_written_as_batches_then_it_is_faster_than_one_upsert_per_term_on_the_same_machine()
    {
        var category = await AddCategoryAsync("Colour");
        var batchTerms = Enumerable.Range(0, 3000).Select(index => Term($"batch{index}", category.Id)).ToArray();
        var singleTerms = Enumerable.Range(0, 3000).Select(index => Term($"single{index}", category.Id)).ToArray();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        foreach (var term in singleTerms)
            _ = Ok(await Database.Terms.UpsertAsync(term, CancellationToken));

        var singleElapsed = clock.Elapsed;
        clock.Restart();

        _ = Ok(await Database.Terms.UpsertManyAsync(batchTerms, CancellationToken));

        clock.Elapsed.ShouldBeLessThan(singleElapsed, $"batch took {clock.Elapsed.TotalMilliseconds:F0} ms, one per term took {singleElapsed.TotalMilliseconds:F0} ms; the ratio is used because absolute time varies on shared CI runners");
    }
}
