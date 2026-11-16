using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Writes the shipped seed into the stores when its version is new. The terms go to the store in batches of <see cref="BatchSize" />.</summary>
/// <param name="seedSource">Loads the seed.</param>
/// <param name="seedState">Remembers the applied version.</param>
/// <param name="categories">Stores categories.</param>
/// <param name="terms">Stores terms.</param>
/// <param name="logger">Reports the outcome without terms or paths.</param>
public sealed class ClassificationSeeder(IClassificationSeedSource seedSource, ISeedStateStore seedState, ICategoryStore categories, ITermStore terms, ILogger<ClassificationSeeder> logger) : IClassificationSeeder
{
    /// <summary>The most terms written in one store call, so one write transaction stays short.</summary>
    public const int BatchSize = 1000;

    /// <inheritdoc />
    public async Task<ClassificationSeedOutcome> SeedAsync(CancellationToken cancellationToken)
    {
        var run = RunAsync(cancellationToken);
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        var outcome = run.IsCompletedSuccessfully ? run.Result : Faulted(run);
        Report(outcome);

        return outcome;
    }

    private ClassificationSeedOutcome Faulted(Task run)
    {
        LogMessage.ExceptionCaught(logger, nameof(ClassificationSeeder), run.Exception?.GetBaseException().ErrorType() ?? nameof(OperationCanceledException));

        return ClassificationSeedOutcomeFactory.CreateFailed(ClassificationFailureFactory.CreateUnavailable());
    }

    private void Report(ClassificationSeedOutcome outcome)
    {
        if (outcome is ClassificationSeedOutcome.Failed failed)
            LogMessage.Warning(logger, nameof(ClassificationSeeder), $"The classification seed was not applied: {failed.Failure.GetType().Name}");
        else
            LogMessage.Information(logger, nameof(ClassificationSeeder), outcome is ClassificationSeedOutcome.Applied ? "The classification seed was applied" : "The classification seed is already current");
    }

    private Task<ClassificationSeedOutcome> RunAsync(CancellationToken cancellationToken) =>
        seedSource.LoadAsync(cancellationToken).MatchAsync(seed => ApplyWhenNewAsync(seed, cancellationToken), failure => Task.FromResult(ClassificationSeedOutcomeFactory.CreateFailed(failure)));

    private Task<ClassificationSeedOutcome> ApplyWhenNewAsync(ClassificationSeed seed, CancellationToken cancellationToken) =>
        seedState.GetAppliedVersionAsync(cancellationToken).MatchAsync(
            applied => applied.Match(version => version == seed.Version, () => false) ? Task.FromResult(ClassificationSeedOutcomeFactory.CreateAlreadyCurrent()) : ApplyAsync(seed, cancellationToken),
            failure => Task.FromResult(ClassificationSeedOutcomeFactory.CreateFailed(failure)));

    private async Task<ClassificationSeedOutcome> ApplyAsync(ClassificationSeed seed, CancellationToken cancellationToken) =>
        await UpsertCategoriesAsync(seed, cancellationToken).MatchAsync(
            ids => WriteTermsAsync(seed, ids, cancellationToken),
            failure => Task.FromResult(ClassificationSeedOutcomeFactory.CreateFailed(failure))).ConfigureAwait(false);

    private async Task<Result<IReadOnlyDictionary<string, CategoryId>, ClassificationFailure>> UpsertCategoriesAsync(ClassificationSeed seed, CancellationToken cancellationToken)
    {
        var ids = new Dictionary<string, CategoryId>(StringComparer.OrdinalIgnoreCase);
        foreach (var categoryName in seed.Categories.Select(category => category.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), categoryName);
            var failure = await categories.UpsertAsync(category, cancellationToken).MatchAsync(stored => Store(ids, categoryName, stored), Option.Some).ConfigureAwait(false);
            if (failure.TryGetValue(out var failed))
                return Result.Failure<IReadOnlyDictionary<string, CategoryId>, ClassificationFailure>(failed);
        }

        return Result.Success<IReadOnlyDictionary<string, CategoryId>, ClassificationFailure>(ids);
    }

    private static Option<ClassificationFailure> Store(Dictionary<string, CategoryId> ids, string seedName, ClassificationCategory stored)
    {
        ids[seedName] = stored.Id;

        return Option.None<ClassificationFailure>();
    }

    private async Task<ClassificationSeedOutcome> WriteTermsAsync(ClassificationSeed seed, IReadOnlyDictionary<string, CategoryId> ids, CancellationToken cancellationToken)
    {
        foreach (var batch in seed.Terms.DistinctBy(term => (term.Text, term.CategoryName.ToUpperInvariant())).Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var failure = await UpsertBatchAsync(batch, ids, cancellationToken).ConfigureAwait(false);
            if (failure.TryGetValue(out var failed))
                return ClassificationSeedOutcomeFactory.CreateFailed(failed);
        }

        return await seedState.SetAppliedVersionAsync(seed.Version, cancellationToken).MatchAsync(_ => ClassificationSeedOutcomeFactory.CreateApplied(), ClassificationSeedOutcomeFactory.CreateFailed).ConfigureAwait(false);
    }

    private async Task<Option<ClassificationFailure>> UpsertBatchAsync(SeedTerm[] batch, IReadOnlyDictionary<string, CategoryId> ids, CancellationToken cancellationToken)
    {
        var batchTerms = new List<ClassificationTerm>(batch.Length);
        foreach (var seedTerm in batch)
        {
            if (!ids.TryGetValue(seedTerm.CategoryName, out var categoryId))
                return Option.Some(ClassificationFailureFactory.CreateInvalidSeed());

            if (ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), seedTerm.Text, categoryId, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateActive()).TryGetValue(out var term))
                batchTerms.Add(term);
        }

        return await terms.UpsertManyAsync(batchTerms, cancellationToken).MatchAsync(_ => Option.None<ClassificationFailure>(), Option.Some).ConfigureAwait(false);
    }
}
