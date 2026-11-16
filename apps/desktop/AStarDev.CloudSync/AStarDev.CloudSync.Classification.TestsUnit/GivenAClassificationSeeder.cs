using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationSeeder
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static SeedTerm Term(string text, string category, SeedTermKind? kind = null) =>
        SeedTermFactory.Create(text, category, kind ?? SeedTermKindFactory.CreateWord()).Match(term => term, () => throw new InvalidOperationException("The test term must be valid."));

    private static ClassificationSeed Seed(string version, params SeedTerm[] terms) =>
        ClassificationSeedFactory.Create(version, [SeedCategoryFactory.Create("Colour"), SeedCategoryFactory.Create("Image")], terms);

    private static ClassificationSeed DefaultSeed(string version = "1") =>
        Seed(version, Term("red", "Colour"), Term("jpg", "Image", SeedTermKindFactory.CreateExtension()), Term("holiday photos", "Image", SeedTermKindFactory.CreateFolder()));

    private static ClassificationSeeder Create(ClassificationSeed seed, SeedingStateStore state, SeedingCategoryStore categories, SeedingTermStore terms, RecordingLogger<ClassificationSeeder>? logger = null) =>
        new(new SeedingSeedSource(Result.Success<ClassificationSeed, ClassificationFailure>(seed)), state, categories, terms, logger ?? new RecordingLogger<ClassificationSeeder>());

    private static ClassificationTerm StoredTerm(string text, CategoryId category, TermSource source, TermStatus status) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, category, source, status).Match(term => term, () => throw new InvalidOperationException("The test term must be valid."));

    private static ClassificationSeed LargeSeed(int termCount) =>
        ClassificationSeedFactory.Create("9", [SeedCategoryFactory.Create("Colour")], [.. Enumerable.Range(0, termCount).Select(index => Term($"term{index}", "Colour"))]);

    [Fact]
    public async Task when_no_version_was_applied_then_everything_is_stored_and_the_version_recorded()
    {
        var state = new SeedingStateStore();
        var categories = new SeedingCategoryStore();
        var terms = new SeedingTermStore();

        var outcome = await Create(DefaultSeed("7"), state, categories, terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        categories.Stored.Select(category => category.Name).ShouldBe(["Colour", "Image"], ignoreOrder: true);
        terms.Stored.Select(term => term.Text).ShouldBe(["red", "jpg", "holiday photos"], ignoreOrder: true);
        terms.Stored.ShouldAllBe(term => term.Source is TermSource.Seed && term.Status is TermStatus.Active);
        state.Version.ShouldBe("7");
    }

    [Fact]
    public async Task when_an_extension_and_a_folder_term_are_seeded_then_they_are_stored_as_the_raw_extension_and_folder_name()
    {
        var terms = new SeedingTermStore();

        _ = await Create(DefaultSeed(), new SeedingStateStore(), new SeedingCategoryStore(), terms).SeedAsync(CancellationToken);

        terms.Stored.Select(term => term.Text).ShouldContain("jpg");
        terms.Stored.Select(term => term.Text).ShouldContain("holiday photos");
    }

    [Fact]
    public async Task when_the_same_version_is_already_applied_then_nothing_is_written()
    {
        var state = new SeedingStateStore { Version = "1" };
        var categories = new SeedingCategoryStore();
        var terms = new SeedingTermStore();

        var outcome = await Create(DefaultSeed("1"), state, categories, terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.AlreadyCurrent>();
        categories.UpsertCalls.ShouldBe(0);
        terms.BatchCalls.ShouldBe(0);
        state.SetCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_seed_runs_twice_then_the_second_run_is_a_no_op()
    {
        var state = new SeedingStateStore();
        var categories = new SeedingCategoryStore();
        var terms = new SeedingTermStore();
        var seeder = Create(DefaultSeed(), state, categories, terms);
        _ = await seeder.SeedAsync(CancellationToken);
        var termCalls = terms.BatchCalls;

        var outcome = await seeder.SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.AlreadyCurrent>();
        terms.BatchCalls.ShouldBe(termCalls);
        terms.Stored.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_the_version_changed_then_the_new_terms_are_added_and_the_existing_ones_stay_single()
    {
        var state = new SeedingStateStore { Version = "1" };
        var categories = new SeedingCategoryStore();
        var terms = new SeedingTermStore();
        _ = await Create(DefaultSeed("1"), new SeedingStateStore(), categories, terms).SeedAsync(CancellationToken);

        var outcome = await Create(Seed("2", Term("red", "Colour"), Term("blue", "Colour")), state, categories, terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        terms.Stored.Count(term => term.Text == "red").ShouldBe(1);
        terms.Stored.Select(term => term.Text).ShouldContain("blue");
        state.Version.ShouldBe("2");
    }

    [Fact]
    public async Task when_a_seeded_term_was_ignored_then_it_stays_ignored_and_is_not_recreated()
    {
        var categories = new SeedingCategoryStore();
        var colour = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), "Colour");
        categories.Add(colour);
        var terms = new SeedingTermStore();
        terms.Add(StoredTerm("red", colour.Id, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored()));

        _ = await Create(DefaultSeed(), new SeedingStateStore(), categories, terms).SeedAsync(CancellationToken);

        terms.Stored.Where(term => term.Text == "red").ShouldHaveSingleItem().Status.ShouldBeOfType<TermStatus.Ignored>();
    }

    [Fact]
    public async Task when_a_user_term_has_the_same_text_then_it_stays_a_user_term()
    {
        var categories = new SeedingCategoryStore();
        var colour = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), "Colour");
        categories.Add(colour);
        var terms = new SeedingTermStore();
        terms.Add(StoredTerm("red", colour.Id, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()));
        terms.Add(StoredTerm("crimson", colour.Id, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()));

        _ = await Create(DefaultSeed(), new SeedingStateStore(), categories, terms).SeedAsync(CancellationToken);

        terms.Stored.Where(term => term.Text == "red").ShouldHaveSingleItem().Source.ShouldBeOfType<TermSource.User>();
        terms.Stored.Select(term => term.Text).ShouldContain("crimson");
    }

    [Fact]
    public async Task when_the_category_already_exists_with_another_id_then_the_terms_use_the_stored_id()
    {
        var categories = new SeedingCategoryStore();
        var colour = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), "colour");
        categories.Add(colour);
        var terms = new SeedingTermStore();

        _ = await Create(DefaultSeed(), new SeedingStateStore(), categories, terms).SeedAsync(CancellationToken);

        terms.Stored.Where(term => term.Text == "red").ShouldHaveSingleItem().CategoryId.ShouldBe(colour.Id);
    }

    [Fact]
    public async Task when_one_text_is_seeded_as_a_word_and_an_extension_of_one_category_then_it_is_stored_once()
    {
        var terms = new SeedingTermStore();
        var seed = Seed("1", Term("jpg", "Image"), Term("jpg", "Image", SeedTermKindFactory.CreateExtension()));

        _ = await Create(seed, new SeedingStateStore(), new SeedingCategoryStore(), terms).SeedAsync(CancellationToken);

        terms.BatchSizes.ShouldBe([1]);
    }

    [Fact]
    public async Task when_the_seed_has_more_terms_than_one_batch_then_they_are_written_in_batches_and_the_version_is_recorded_after_all_of_them()
    {
        var state = new SeedingStateStore();
        var terms = new SeedingTermStore();

        var outcome = await Create(LargeSeed((ClassificationSeeder.BatchSize * 2) + 500), state, new SeedingCategoryStore(), terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        terms.BatchSizes.ShouldBe([ClassificationSeeder.BatchSize, ClassificationSeeder.BatchSize, 500]);
        terms.Stored.Count.ShouldBe((ClassificationSeeder.BatchSize * 2) + 500);
        (state.Version, state.SetCalls).ShouldBe(("9", 1));
    }

    [Fact]
    public async Task when_the_seed_fits_one_batch_then_one_store_call_is_made()
    {
        var terms = new SeedingTermStore();

        _ = await Create(DefaultSeed(), new SeedingStateStore(), new SeedingCategoryStore(), terms).SeedAsync(CancellationToken);

        terms.BatchSizes.ShouldBe([3]);
    }

    [Fact]
    public async Task when_a_batch_fails_then_the_run_fails_no_later_batch_is_written_and_the_version_is_not_recorded()
    {
        var state = new SeedingStateStore();
        var terms = new SeedingTermStore { FailFromBatch = 2 };

        var outcome = await Create(LargeSeed((ClassificationSeeder.BatchSize * 2) + 500), state, new SeedingCategoryStore(), terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>().Failure.ShouldBeOfType<ClassificationFailure.Unavailable>();
        state.SetCalls.ShouldBe(0);
        terms.BatchCalls.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_category_upsert_fails_then_the_run_fails_and_no_term_is_written()
    {
        var state = new SeedingStateStore();
        var terms = new SeedingTermStore();

        var outcome = await Create(DefaultSeed(), state, new SeedingCategoryStore { FailUpserts = true }, terms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
        terms.BatchCalls.ShouldBe(0);
        state.SetCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_failed_run_is_repeated_then_it_completes_and_records_the_version()
    {
        var state = new SeedingStateStore();
        var categories = new SeedingCategoryStore();
        var terms = new SeedingTermStore { FailFromBatch = 2 };
        _ = await Create(LargeSeed(ClassificationSeeder.BatchSize + 5), state, categories, terms).SeedAsync(CancellationToken);
        var healthyTerms = new SeedingTermStore();
        foreach (var term in terms.Stored)
            healthyTerms.Add(term);

        var outcome = await Create(LargeSeed(ClassificationSeeder.BatchSize + 5), state, categories, healthyTerms).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Applied>();
        healthyTerms.Stored.Count.ShouldBe(ClassificationSeeder.BatchSize + 5);
        state.Version.ShouldBe("9");
    }

    [Fact]
    public async Task when_the_seed_cannot_be_loaded_then_the_run_fails_with_that_failure()
    {
        var state = new SeedingStateStore();
        var categories = new SeedingCategoryStore();
        var source = new SeedingSeedSource(Result.Failure<ClassificationSeed, ClassificationFailure>(ClassificationFailureFactory.CreateInvalidSeed()));
        var seeder = new ClassificationSeeder(source, state, categories, new SeedingTermStore(), new RecordingLogger<ClassificationSeeder>());

        var outcome = await seeder.SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>().Failure.ShouldBeOfType<ClassificationFailure.InvalidSeed>();
        categories.UpsertCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_applied_version_cannot_be_read_then_the_run_fails_and_nothing_is_written()
    {
        var categories = new SeedingCategoryStore();

        var outcome = await Create(DefaultSeed(), new SeedingStateStore { FailReads = true }, categories, new SeedingTermStore()).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
        categories.UpsertCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_recording_the_version_fails_then_the_run_fails()
    {
        var outcome = await Create(DefaultSeed(), new SeedingStateStore { FailWrites = true }, new SeedingCategoryStore(), new SeedingTermStore()).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
    }

    [Fact]
    public async Task when_the_token_is_cancelled_before_the_run_then_the_run_fails_without_throwing_and_the_version_is_not_recorded()
    {
        var state = new SeedingStateStore();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var outcome = await Create(DefaultSeed(), state, new SeedingCategoryStore(), new SeedingTermStore()).SeedAsync(cancellation.Token);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
        state.SetCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_during_the_term_writes_then_the_run_stops_and_the_version_is_not_recorded()
    {
        var state = new SeedingStateStore();
        using var cancellation = new CancellationTokenSource();
        var terms = new SeedingTermStore(call =>
        {
            if (call == 2)
                cancellation.Cancel();
        });

        var outcome = await Create(LargeSeed((ClassificationSeeder.BatchSize * 2) + 500), state, new SeedingCategoryStore(), terms).SeedAsync(cancellation.Token);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
        state.SetCalls.ShouldBe(0);
        terms.BatchCalls.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_store_throws_then_the_run_fails_without_throwing()
    {
        var state = new SeedingStateStore();

        var outcome = await Create(DefaultSeed(), state, new SeedingCategoryStore { ThrowOnUpsert = true }, new SeedingTermStore()).SeedAsync(CancellationToken);

        outcome.ShouldBeOfType<ClassificationSeedOutcome.Failed>();
        state.SetCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_seed_is_applied_then_the_logs_carry_no_term_text()
    {
        var logger = new RecordingLogger<ClassificationSeeder>();

        _ = await Create(DefaultSeed(), new SeedingStateStore(), new SeedingCategoryStore(), new SeedingTermStore(), logger).SeedAsync(CancellationToken);

        logger.Messages.ShouldNotBeEmpty();
        logger.Messages.ShouldAllBe(message => !message.Contains("holiday") && !message.Contains("red") && !message.Contains("jpg"));
    }

    [Fact]
    public async Task when_the_run_fails_then_a_warning_is_logged_without_term_text()
    {
        var logger = new RecordingLogger<ClassificationSeeder>();

        _ = await Create(DefaultSeed(), new SeedingStateStore(), new SeedingCategoryStore(), new SeedingTermStore { FailFromBatch = 1 }, logger).SeedAsync(CancellationToken);

        logger.Entries.ShouldContain(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("holiday") && !message.Contains("red"));
    }
}
