using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="readers">What is read before a pass starts.</param>
/// <param name="stagger">Spreads the accounts across the interval.</param>
/// <param name="runner">Runs the passes.</param>
/// <param name="options">Startup delay and check period.</param>
/// <param name="timeProvider">Supplies the clock and the timer.</param>
/// <param name="logger">Logs skipped accounts and the reason, without account details.</param>
internal sealed class SyncScheduler(SyncSchedulerReaders readers, IStaggerPolicy stagger, ISyncPassRunner runner, SyncSchedulerOptions options, TimeProvider timeProvider, ILogger<SyncScheduler> logger) : ISyncScheduler, IDisposable
{
    private const string Location = nameof(SyncScheduler);
    private const string UnreadableMessage = "The accounts or the paused accounts could not be read, so no scheduled passes were started.";

    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, DateTimeOffset> due = [];
    private readonly ConcurrentDictionary<Task, byte> passes = new();
    private ITimer? timer;
    private CancellationTokenSource? running;
    private DateTimeOffset anchor;
    private DateTimeOffset lastChecked;
    private TimeSpan dueInterval;
    private int checking;
    private int recheck;

    /// <summary>Releases the cancellation source of a scheduler that was not stopped.</summary>
    public void Dispose() => running?.Dispose();

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (timer is not null)
                return Task.CompletedTask;

            anchor = timeProvider.GetUtcNow();
            lastChecked = anchor;
            running = new CancellationTokenSource();
            var token = running.Token;
            timer = timeProvider.CreateTimer(_ => OnTick(token), null, options.CheckEvery, options.CheckEvery);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ITimer? stopping;
        CancellationTokenSource? cancelling;
        lock (gate)
        {
            stopping = timer;
            cancelling = running;
            timer = null;
            running = null;
        }

        if (stopping is null || cancelling is null)
            return;

        await stopping.DisposeAsync().ConfigureAwait(false);
        await cancelling.CancelAsync().ConfigureAwait(false);
        await Task.WhenAll(passes.Keys).WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        cancelling.Dispose();
        due.Clear();
        _ = Interlocked.Exchange(ref checking, 0);
        _ = Interlocked.Exchange(ref recheck, 0);
    }

    private static DateTimeOffset FirstSlotAtOrAfter(DateTimeOffset first, TimeSpan interval, DateTimeOffset moment) =>
        moment <= first ? first : first + (interval * Math.Ceiling((moment - first) / interval));

    private static DateTimeOffset FirstSlotAfter(DateTimeOffset first, TimeSpan interval, DateTimeOffset moment) =>
        moment < first ? first : first + (interval * (Math.Floor((moment - first) / interval) + 1));

    private void OnTick(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        if (Interlocked.CompareExchange(ref checking, 1, 0) != 0)
        {
            _ = Interlocked.Exchange(ref recheck, 1);

            return;
        }

        _ = Task.Run(() => CheckAndReleaseAsync(token), token);
    }

    private async Task CheckAndReleaseAsync(CancellationToken token)
    {
        do
        {
            do
            {
                _ = Interlocked.Exchange(ref recheck, 0);
                _ = await Try.RunAsync(() => CheckAsync(token), token).MatchAsync(done => done, LogCheckFailed).ConfigureAwait(false);
            }
            while (Volatile.Read(ref recheck) == 1);

            _ = Interlocked.Exchange(ref checking, 0);
        }
        while (Volatile.Read(ref recheck) == 1 && Interlocked.CompareExchange(ref checking, 1, 0) == 0);
    }

    private Task<Unit> CheckAsync(CancellationToken token) =>
        readers.Settings.GetAsync(token).MatchAsync(schedule => ApplyAsync(schedule, token), _ => Task.FromResult(Warn("The sync schedule could not be read, so no scheduled passes were started.")));

    private Task<Unit> ApplyAsync(SyncSchedule schedule, CancellationToken token)
    {
        if (schedule is not SyncSchedule.Every every)
        {
            due.Clear();

            return Task.FromResult(Unit.Instance);
        }

        return readers.Accounts.GetAllAsync(token)
            .MatchAsync(
                all => readers.Paused.GetPausedAsync(token).MatchAsync(pausedIds => StartDueAsync(every.Interval, all, pausedIds, token), _ => Task.FromResult(Warn(UnreadableMessage))),
                _ => Task.FromResult(Warn(UnreadableMessage)));
    }

    private async Task<Unit> StartDueAsync(TimeSpan interval, IReadOnlyList<StoredAccount> all, IReadOnlyList<AccountId> pausedIds, CancellationToken token)
    {
        var now = timeProvider.GetUtcNow();
        var ids = all.Select(account => account.Id).ToList();
        var offsets = stagger.OffsetsFor(ids, interval);
        ForgetStaleDueTimes(interval, ids);

        foreach (var accountId in ids.Distinct())
        {
            var first = anchor + options.StartupDelay + offsets[accountId];
            var next = NextDue(accountId, first, interval, now);

            if (now < next)
            {
                due[accountId] = next;

                continue;
            }

            due[accountId] = FirstSlotAfter(first, interval, now);
            await StartOrSkipAsync(accountId, pausedIds, token).ConfigureAwait(false);
        }

        lastChecked = now;

        return Unit.Instance;
    }

    private DateTimeOffset NextDue(AccountId accountId, DateTimeOffset first, TimeSpan interval, DateTimeOffset now)
    {
        if (due.TryGetValue(accountId, out var known))
            return known;

        return first > lastChecked ? first : FirstSlotAtOrAfter(first, interval, now);
    }

    private void ForgetStaleDueTimes(TimeSpan interval, List<AccountId> ids)
    {
        if (dueInterval != interval)
        {
            due.Clear();
            dueInterval = interval;
        }

        foreach (var gone in due.Keys.Where(accountId => !ids.Contains(accountId)).ToList())
            _ = due.Remove(gone);
    }

    private async Task StartOrSkipAsync(AccountId accountId, IReadOnlyList<AccountId> pausedIds, CancellationToken token)
    {
        var skipReason = await SkipReasonAsync(accountId, pausedIds, token).ConfigureAwait(false);
        _ = skipReason.Match(reason => Info($"A scheduled sync was skipped: {reason}"), () => Start(accountId, token));
    }

    private async Task<Option<string>> SkipReasonAsync(AccountId accountId, IReadOnlyList<AccountId> pausedIds, CancellationToken token)
    {
        if (pausedIds.Contains(accountId))
            return Option.Some("the account is paused.");

        if (readers.SignIn.StateOf(accountId) is AccountSignInState.SignInRequired)
            return Option.Some("the account needs to sign in.");

        if (readers.Activity.ActivityOf(accountId) is AccountSyncActivity.Running)
            return Option.Some("the account is already syncing.");

        return await readers.Roots.GetAsync(accountId, token).MatchAsync(RootReason, _ => Option.Some("the sync folder could not be read.")).ConfigureAwait(false);
    }

    private static Option<string> RootReason(Option<SyncRoot> root) =>
        root.Match(
            found =>
            {
                found.Directory.Refresh();

                return found.Directory.Exists ? Option.None<string>() : Option.Some("the sync folder is missing.");
            },
            () => Option.Some("the account has no sync folder."));

    private Unit Start(AccountId accountId, CancellationToken token)
    {
        var pass = RunPassAsync(accountId, token);
        _ = passes.TryAdd(pass, 0);
        _ = pass.ContinueWith(finished => passes.TryRemove(finished, out _), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return Unit.Instance;
    }

    private Task<Unit> RunPassAsync(AccountId accountId, CancellationToken token) =>
        Try.RunAsync(() => runner.RunAsync(accountId, SyncTriggerFactory.CreateScheduled(), token), token).MatchAsync(_ => Unit.Instance, LogPassFailed);

    private Unit LogPassFailed(Exception exception) => Warn($"A scheduled sync pass threw {exception.ErrorType()}.");

    private Unit LogCheckFailed(Exception exception) => Warn($"A schedule check threw {exception.ErrorType()}.");

    private Unit Info(string message)
    {
        LogMessage.Information(logger, Location, message);

        return Unit.Instance;
    }

    private Unit Warn(string message)
    {
        LogMessage.Warning(logger, Location, message);

        return Unit.Instance;
    }
}
