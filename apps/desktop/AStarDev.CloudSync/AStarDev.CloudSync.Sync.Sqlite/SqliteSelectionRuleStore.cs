using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the accounts' selection rules in the sync tables. A save reads the rules, plans the staged changes and writes the result inside one immediate transaction, so concurrent saves for an account run one after the other.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteSelectionRuleStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSelectionRuleStore> logger) : ISelectionRuleReader, ISelectionRuleWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteSelectionRuleStore);
    private const string TwoWay = "TwoWay";
    private const string UploadOnly = "UploadOnly";
    private const string DownloadOnly = "DownloadOnly";

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(
                entities => Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(Sorted(entities)),
                exception => Failed<IReadOnlyList<SelectionRule>>(exception, "The selection rules could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken) =>
        Try.RunAsync(() => PlanAndSaveAsync(accountId, changes, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The selection rules could not be saved."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's selection rules could not be removed."));

    private static IReadOnlyList<SelectionRule> Sorted(List<SelectionRuleEntity> entities) =>
        [.. entities.SelectMany(ToRule).OrderBy(rule => rule.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)];

    private static IEnumerable<SelectionRule> ToRule(SelectionRuleEntity entity) =>
        LocalRelativePathFactory.Create(entity.Path).Match(
            path => new[] { SelectionRuleFactory.Create(AccountIdFactory.Create(entity.AccountId), path, RemoteItemIdFactory.Create(entity.RemoteId).Match(Option.Some, _ => Option.None<RemoteItemId>()), KindOf(entity)) },
            _ => []);

    private static SelectionRuleKind KindOf(SelectionRuleEntity entity) => entity.IsInclude ? SelectionRuleKindFactory.CreateInclude(DirectionOf(entity.Direction)) : SelectionRuleKindFactory.CreateExclude();

    private static SyncDirection DirectionOf(string text) => text switch
    {
        UploadOnly => SyncDirectionFactory.CreateUploadOnly(),
        DownloadOnly => SyncDirectionFactory.CreateDownloadOnly(),
        _ => SyncDirectionFactory.CreateTwoWay(),
    };

    private static SelectionRuleEntity ToEntity(SelectionRule rule) => new()
    {
        AccountId = rule.AccountId.Value,
        Path = rule.Path.Value ?? string.Empty,
        RemoteId = rule.RemoteId.Match(id => (string?)id.Value, () => null),
        IsInclude = rule.Kind is SelectionRuleKind.Include,
        Direction = rule.Kind is SelectionRuleKind.Include include ? NameOf(include.Direction) : TwoWay,
    };

    private static string NameOf(SyncDirection direction) => direction switch
    {
        SyncDirection.UploadOnly => UploadOnly,
        SyncDirection.DownloadOnly => DownloadOnly,
        _ => TwoWay,
    };

    private async Task<List<SelectionRuleEntity>> ReadAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SelectionRules.AsNoTracking().Where(rule => rule.AccountId == accountId.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> PlanAndSaveAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var stored = await dbContext.SelectionRules.AsNoTracking().Where(rule => rule.AccountId == accountId.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
        var planned = SelectionRulePlanner.Apply(accountId, Sorted(stored), changes);
        _ = await dbContext.SelectionRules.Where(rule => rule.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        dbContext.SelectionRules.AddRange(planned.Select(ToEntity));
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SelectionRules.Where(rule => rule.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
