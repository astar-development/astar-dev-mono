namespace AStarDev.CloudSync.Sync;

/// <summary>Releases a semaphore when disposed, so a <c>using</c> holds it for a scope.</summary>
/// <param name="gate">The semaphore to release.</param>
internal sealed class SemaphoreRelease(SemaphoreSlim gate) : IDisposable
{
    public void Dispose() => gate.Release();
}
