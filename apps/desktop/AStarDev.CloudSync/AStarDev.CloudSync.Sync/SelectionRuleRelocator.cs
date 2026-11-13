using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Moves selection rules to follow folders that were renamed or moved remotely (#1398).</summary>
/// <param name="indexReader">Computes each folder's current path.</param>
/// <param name="ruleReader">Reads the rules.</param>
/// <param name="ruleWriter">Saves the relocations in one transaction.</param>
/// <param name="logger">Reports failures by exception type only.</param>
internal sealed class SelectionRuleRelocator(IRemoteIndexReader indexReader, ISelectionRuleReader ruleReader, ISelectionRuleWriter ruleWriter, ILogger<SelectionRuleRelocator> logger) : ISelectionRuleRelocator
{
    private const string Location = nameof(SelectionRuleRelocator);

    /// <inheritdoc />
    public Task<Result<RuleRelocationSummary, ErrorResponse>> RelocateAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => PlanAndSaveAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(summary => Result.Success<RuleRelocationSummary, ErrorResponse>(summary), exception => Failed(exception));

    private async Task<RuleRelocationSummary> PlanAndSaveAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var rules = (await ruleReader.GetAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var moves = new List<FolderMove>();
        var dropped = 0;

        foreach (var rule in rules)
        {
            var target = await TargetOfAsync(accountId, rule, cancellationToken).ConfigureAwait(false);

            if (target.Equals(rule.Path))
                continue;

            moves.Add(FolderMoveFactory.Create(rule.Path, target));
            dropped += rules.Any(other => other.Path.Equals(target)) ? 1 : 0;
        }

        if (moves.Count > 0)
            _ = (await ruleWriter.ApplyAsync(accountId, [SelectionChangeFactory.CreateRelocateFolders(moves)], cancellationToken).ConfigureAwait(false)).OrThrow();

        return RuleRelocationSummaryFactory.Create(moves.Count - dropped, dropped);
    }

    private async Task<LocalRelativePath> TargetOfAsync(AccountId accountId, SelectionRule rule, CancellationToken cancellationToken)
    {
        if (rule.RemoteId.Match(_ => false, () => true))
            return rule.Path;

        var found = await rule.RemoteId.Match(id => indexReader.GetPathAsync(accountId, id, cancellationToken), () => Task.FromResult(Result.Success<Option<LocalRelativePath>, ErrorResponse>(Option.None<LocalRelativePath>()))).ConfigureAwait(false);

        return found.OrThrow().Match(path => path, () => rule.Path);
    }

    private Result<RuleRelocationSummary, ErrorResponse> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<RuleRelocationSummary, ErrorResponse>(new ErrorResponse("The selection rules could not be relocated."));
    }
}
