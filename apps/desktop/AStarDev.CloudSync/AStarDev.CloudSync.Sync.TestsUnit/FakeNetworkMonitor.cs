namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeNetworkMonitor : INetworkMonitor
{
    private NetworkAvailability availability = NetworkAvailabilityFactory.CreateAvailable();

    public event EventHandler? Changed;

    public NetworkAvailability Availability => Volatile.Read(ref availability);

    public int Subscribers => Changed?.GetInvocationList().Length ?? 0;

    public void Set(NetworkAvailability next)
    {
        Volatile.Write(ref availability, next);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
