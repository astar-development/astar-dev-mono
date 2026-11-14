using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public sealed class FakeDatabaseHealthGate : IDatabaseHealthGate
{
    public event EventHandler? Changed;

    public DatabaseHealthStatus Status { get; private set; } = DatabaseHealthStatusFactory.CreateNotChecked();

    public Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(Status);

    public void Finish(DatabaseHealthStatus status)
    {
        Status = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
