using AStarDev.CloudSync.Infrastructure.OneDrive;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeMsalTokenCacheStorage : IMsalTokenCacheStorage
{
    private int secureStoreAttempts;
    private int plaintextFileAttempts;
    private int moves;
    private int detaches;

    public bool SecureStoreFails { get; init; }

    public bool PlaintextFileFails { get; init; }

    public bool PlaintextFileOnDisk { get; set; }

    public bool MoveFails { get; init; }

    public bool DetachFails { get; init; }

    public TimeSpan AttachDelay { get; init; } = TimeSpan.Zero;

    public int SecureStoreAttempts => secureStoreAttempts;

    public int PlaintextFileAttempts => plaintextFileAttempts;

    public int Moves => moves;

    public int Detaches => detaches;

    public async Task AttachSecureStoreAsync()
    {
        _ = Interlocked.Increment(ref secureStoreAttempts);
        await Task.Delay(AttachDelay);

        if (SecureStoreFails)
            throw new InvalidOperationException("The keyring is locked.");
    }

    public Task AttachPlaintextFileAsync()
    {
        _ = Interlocked.Increment(ref plaintextFileAttempts);

        return PlaintextFileFails ? Task.FromException(new IOException("The cache file is read-only.")) : Task.CompletedTask;
    }

    public Task<PlaintextTokenMove> MovePlaintextFileToSecureStoreAsync()
    {
        _ = Interlocked.Increment(ref moves);

        if (MoveFails)
            return Task.FromException<PlaintextTokenMove>(new IOException("The keyring refused the write."));

        if (!PlaintextFileOnDisk)
            return Task.FromResult(PlaintextTokenMoveFactory.CreateNothingToMove());

        PlaintextFileOnDisk = false;

        return Task.FromResult(PlaintextTokenMoveFactory.CreateMoved());
    }

    public Task DetachPlaintextFileAsync()
    {
        _ = Interlocked.Increment(ref detaches);

        if (DetachFails)
            return Task.FromException(new IOException("The cache file is locked."));

        PlaintextFileOnDisk = false;

        return Task.CompletedTask;
    }
}
