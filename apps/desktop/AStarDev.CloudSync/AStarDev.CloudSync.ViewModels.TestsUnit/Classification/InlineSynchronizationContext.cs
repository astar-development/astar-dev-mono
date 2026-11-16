namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

internal sealed class InlineSynchronizationContext : SynchronizationContext
{
    private readonly object gate = new();
    private int posts;

    internal int Posts
    {
        get
        {
            lock (gate)
                return posts;
        }
    }

    internal static Installed Install(InlineSynchronizationContext context)
    {
        var previous = Current;
        SetSynchronizationContext(context);

        return new Installed(previous);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (gate)
            posts++;

        d(state);
    }

    internal sealed class Installed(SynchronizationContext? previous) : IDisposable
    {
        public void Dispose() => SetSynchronizationContext(previous);
    }
}
