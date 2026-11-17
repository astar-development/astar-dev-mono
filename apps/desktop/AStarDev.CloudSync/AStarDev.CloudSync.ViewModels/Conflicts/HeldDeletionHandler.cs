using System.Collections.Concurrent;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using AStarDev.CloudSync.ViewModels.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal sealed class HeldDeletionHandler
{
    private readonly IHeldDeletionDecider decider;
    private readonly ILocalizationService localization;
    private readonly ILogger logger;
    private readonly Func<CancellationToken, Task> reload;
    private readonly ConcurrentDictionary<HeldDeletionId, string> errorKeys = new();
    private readonly ConcurrentDictionary<HeldDeletionId, byte> deferred = new();
    private readonly ConcurrentDictionary<HeldDeletionId, byte> expanded = new();
    private readonly HeldDeletionActions actions;

    internal HeldDeletionHandler(IHeldDeletionDecider decider, ILocalizationService localization, ILogger logger, Func<CancellationToken, Task> reload)
    {
        this.decider = decider;
        this.localization = localization;
        this.logger = logger;
        this.reload = reload;
        actions = new HeldDeletionActions(ConfirmAsync, RestoreAsync, DecideLater, expanded.ContainsKey, RememberExpanded);
    }

    internal HeldDeletionItemViewModel CreateItem(string accountName, IReadOnlyDictionary<SyncPairId, string> pairNames, SyncAttentionItem.HeldDeletions held)
    {
        var pairName = pairNames.TryGetValue(held.PairId, out var name) ? name : localization.GetLocal("Held.PairUnknown");

        return new HeldDeletionItemViewModel(held, accountName, pairName, actions, errorKeys.TryGetValue(held.Id, out var key) ? Option.Some(key) : Option.None<string>(), deferred.ContainsKey(held.Id), localization);
    }

    internal void Prune(IReadOnlySet<HeldDeletionId> open)
    {
        foreach (var id in errorKeys.Keys.Where(id => !open.Contains(id)))
            _ = errorKeys.TryRemove(id, out _);

        foreach (var id in deferred.Keys.Where(id => !open.Contains(id)))
            _ = deferred.TryRemove(id, out _);

        foreach (var id in expanded.Keys.Where(id => !open.Contains(id)))
            _ = expanded.TryRemove(id, out _);
    }

    private Task ConfirmAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken) => SettleAsync(item, () => decider.ConfirmAsync(item.AccountId, item.Id, cancellationToken), cancellationToken);

    private Task RestoreAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken) => SettleAsync(item, () => decider.RestoreAsync(item.AccountId, item.Id, cancellationToken), cancellationToken);

    private async Task SettleAsync(HeldDeletionItemViewModel item, Func<Task<Result<Unit, HeldDeletionRefusal>>> settle, CancellationToken cancellationToken) =>
        await await UiResumingTry.RunTreatingTimeoutAsFailureAsync(settle, result => result.Match(_ => SettledAsync(item, cancellationToken), refusal => NotSettledAsync(item, refusal)), exception => ThrewAsync(item, exception), cancellationToken);

    private Task SettledAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken)
    {
        _ = errorKeys.TryRemove(item.Id, out _);
        _ = deferred.TryRemove(item.Id, out _);

        return reload(cancellationToken);
    }

    private Task NotSettledAsync(HeldDeletionItemViewModel item, HeldDeletionRefusal refusal)
    {
        ConflictListLog.HeldNotSettled(logger);
        Remember(item, ConflictTexts.HeldRefusedKey(refusal));

        return Task.CompletedTask;
    }

    private Task ThrewAsync(HeldDeletionItemViewModel item, Exception exception)
    {
        ConflictListLog.HeldThrew(logger, exception.ErrorType());
        Remember(item, ConflictTexts.UnexpectedKey);

        return Task.CompletedTask;
    }

    private void Remember(HeldDeletionItemViewModel item, string key)
    {
        errorKeys[item.Id] = key;
        item.ShowError(key);
    }

    private void RememberExpanded(HeldDeletionId id, bool isExpanded)
    {
        if (isExpanded)
            _ = expanded.TryAdd(id, 0);
        else
            _ = expanded.TryRemove(id, out _);
    }

    private void DecideLater(HeldDeletionItemViewModel item)
    {
        _ = deferred.TryAdd(item.Id, 0);
        item.MarkDeferred();
    }
}
