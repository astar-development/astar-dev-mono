using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="check">The integrity check.</param>
internal sealed class DatabaseHealthGate(IDatabaseHealthCheck check) : IDatabaseHealthGate, IDisposable
{
    private readonly SemaphoreSlim turn = new(1, 1);
    private DatabaseHealthStatus status = DatabaseHealthStatusFactory.CreateNotChecked();

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public DatabaseHealthStatus Status => Volatile.Read(ref status);

    /// <inheritdoc />
    public async Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken)
    {
        if (Status is not DatabaseHealthStatus.NotChecked)
            return Status;

        var entering = turn.WaitAsync(cancellationToken);
        await entering.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (!entering.IsCompletedSuccessfully)
            return Status;

        var ran = Status is DatabaseHealthStatus.NotChecked && await RunCheckAsync(cancellationToken).ConfigureAwait(false);
        _ = turn.Release();
        if (ran)
            Changed?.Invoke(this, EventArgs.Empty);

        return Status;
    }

    /// <inheritdoc />
    public void Dispose() => turn.Dispose();

    private static DatabaseHealthStatus StatusOf(Result<Unit, DatabaseHealthFailure> answer) =>
        answer.Match(_ => DatabaseHealthStatusFactory.CreateHealthy(), DatabaseHealthStatusFactory.CreateFailed);

    private static bool IsTransient(Task<Result<Unit, DatabaseHealthFailure>> attempt) =>
        !attempt.IsCompletedSuccessfully || attempt.Result.Match(_ => false, failure => failure is DatabaseHealthFailure.Unreadable);

    private async Task<Task<Result<Unit, DatabaseHealthFailure>>> AttemptAsync(CancellationToken cancellationToken)
    {
        var checking = Task.Run(() => check.CheckAsync(cancellationToken), cancellationToken);
        await ((Task)checking).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return checking;
    }

    private async Task<bool> RunCheckAsync(CancellationToken cancellationToken)
    {
        var checking = await AttemptAsync(cancellationToken).ConfigureAwait(false);
        if (!checking.IsCanceled && IsTransient(checking))
            checking = await AttemptAsync(cancellationToken).ConfigureAwait(false);

        if (checking.IsCanceled)
            return false;

        Volatile.Write(ref status, checking.IsCompletedSuccessfully ? StatusOf(checking.Result) : DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()));

        return true;
    }
}
