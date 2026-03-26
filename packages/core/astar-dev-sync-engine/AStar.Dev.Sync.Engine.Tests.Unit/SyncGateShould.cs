using AStar.Dev.Sync.Engine;

namespace AStar.Dev.Sync.Engine.Tests.Unit;

public sealed class SyncGateShould
{
    private readonly SyncGate _sut = new();

    [Fact]
    public void AcquireLockForNewAccount()
    {
        _sut.TryAcquire("acct-1").ShouldBeTrue();
    }

    [Fact]
    public void RejectSecondAcquireForSameAccount()
    {
        _sut.TryAcquire("acct-1");

        _sut.TryAcquire("acct-1").ShouldBeFalse();
    }

    [Fact]
    public void AllowAcquireAfterRelease()
    {
        _sut.TryAcquire("acct-1");
        _sut.Release("acct-1");

        _sut.TryAcquire("acct-1").ShouldBeTrue();
    }

    [Fact]
    public void AllowConcurrentLocksForDifferentAccounts()
    {
        _sut.TryAcquire("acct-1").ShouldBeTrue();
        _sut.TryAcquire("acct-2").ShouldBeTrue();
    }

    [Fact]
    public void ReportIsRunningWhenLocked()
    {
        _sut.TryAcquire("acct-1");

        _sut.IsRunning("acct-1").ShouldBeTrue();
    }

    [Fact]
    public void ReportNotRunningWhenReleased()
    {
        _sut.TryAcquire("acct-1");
        _sut.Release("acct-1");

        _sut.IsRunning("acct-1").ShouldBeFalse();
    }

    [Fact]
    public void ReportNotRunningForUnknownAccount()
    {
        _sut.IsRunning("unknown").ShouldBeFalse();
    }

    [Fact]
    public void BeCaseInsensitive()
    {
        _sut.TryAcquire("Acct-1");

        _sut.TryAcquire("acct-1").ShouldBeFalse();
        _sut.IsRunning("ACCT-1").ShouldBeTrue();
    }

    [Fact]
    public void NotThrowWhenReleasingUnknownAccount()
    {
        Should.NotThrow(() => _sut.Release("never-acquired"));
    }
}
