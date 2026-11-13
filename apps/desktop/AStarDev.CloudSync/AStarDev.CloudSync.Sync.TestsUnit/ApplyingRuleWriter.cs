using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ApplyingRuleWriter(ScriptedRuleReader reader) : ISelectionRuleWriter
{
    private readonly List<SelectionChange> applied = [];

    public bool Fails { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<SelectionChange.IncludeFolder> Includes => [.. applied.OfType<SelectionChange.IncludeFolder>()];

    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        Calls++;
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        applied.AddRange(changes);
        var rules = reader.Listing.Match(listing => listing, _ => []).ToList();
        foreach (var include in changes.OfType<SelectionChange.IncludeFolder>().Where(include => !rules.Any(rule => string.Equals(rule.Path.Value, include.Path.Value, StringComparison.OrdinalIgnoreCase))))
            rules.Add(SelectionRuleFactory.Create(accountId, include.Path, include.RemoteId, SelectionRuleKindFactory.CreateInclude(include.Direction)));

        reader.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(rules);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
