using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GatedActivityLogWriter(IActivityLogWriter inner) : IActivityLogWriter
{
    private readonly TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Reached => reached.Task;

    public void Release() => released.SetResult();

    public async Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken)
    {
        reached.SetResult();
        await released.Task.ConfigureAwait(false);

        return await inner.AppendAsync(entries, cancellationToken).ConfigureAwait(false);
    }
}
