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
        await ((Task)entering).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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

    private async Task<bool> RunCheckAsync(CancellationToken cancellationToken)
    {
        var checking = check.CheckAsync(cancellationToken);
        await ((Task)checking).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (checking.IsCanceled)
            return false;

        Volatile.Write(ref status, checking.IsCompletedSuccessfully ? StatusOf(checking.Result) : DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()));

        return true;
    }
}
