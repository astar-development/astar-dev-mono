using AStar.Dev.OneDriveSync.Infrastructure.SingleInstance;

namespace AStar.Dev.OneDriveSync.Tests.Unit.SingleInstance;

public sealed class GivenASingleInstanceGuard
{
    [Fact]
    public void when_no_instance_is_running_then_acquire_succeeds()
    {
        var mutexName = UniqueMutexName();
        using var sut = new SingleInstanceGuard(mutexName);

        var result = sut.TryAcquire();

        result.ShouldBe(SingleInstanceResult.Acquired);
    }

    [Fact]
    public void when_an_instance_is_already_running_then_acquire_returns_already_running()
    {
        var mutexName    = UniqueMutexName();
        using var firstAcquired = new ManualResetEventSlim();
        using var checkDone     = new ManualResetEventSlim();
        SingleInstanceResult secondResult = default;

        // Named Mutex is thread-affine: it must be released by the thread that acquired it.
        // We therefore use explicit threads so each guard lives entirely on its own thread,
        // avoiding the "unsynchronized block" exception that occurs when async continuations
        // switch threads between acquire and release.
        var holderThread = new Thread(() =>
        {
            using var first = new SingleInstanceGuard(mutexName);
            first.TryAcquire();
            firstAcquired.Set();
            checkDone.Wait(); // hold the mutex until the second thread has finished checking
        });

        var checkerThread = new Thread(() =>
        {
            firstAcquired.Wait();
            using var second = new SingleInstanceGuard(mutexName);
            secondResult = second.TryAcquire();
            checkDone.Set();
        });

        holderThread.Start();
        checkerThread.Start();
        holderThread.Join();
        checkerThread.Join();

        secondResult.ShouldBe(SingleInstanceResult.AlreadyRunning);
    }

    [Fact]
    public void when_the_first_instance_is_disposed_then_a_new_instance_can_acquire()
    {
        var mutexName = UniqueMutexName();

        using (var first = new SingleInstanceGuard(mutexName))
        {
            first.TryAcquire();
        }

        using var second = new SingleInstanceGuard(mutexName);
        var result = second.TryAcquire();

        result.ShouldBe(SingleInstanceResult.Acquired);
    }

    private static string UniqueMutexName() =>
        $"Test.SingleInstance.{Guid.NewGuid():N}";
}
