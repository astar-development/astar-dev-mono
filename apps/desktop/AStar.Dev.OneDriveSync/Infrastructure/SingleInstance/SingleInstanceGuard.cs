using System;
using System.Threading;

namespace AStar.Dev.OneDriveSync.Infrastructure.SingleInstance;

public sealed class SingleInstanceGuard(string mutexName) : IDisposable
{
    private Mutex? _mutex;
    private bool   _ownsMutex;

    public SingleInstanceResult TryAcquire()
    {
        _mutex     = new Mutex(initiallyOwned: false, name: mutexName);
        _ownsMutex = _mutex.WaitOne(millisecondsTimeout: 0);

        return _ownsMutex ? SingleInstanceResult.Acquired : SingleInstanceResult.AlreadyRunning;
    }

    public void Dispose()
    {
        if (_ownsMutex)
            _mutex?.ReleaseMutex();

        _mutex?.Dispose();
    }
}
