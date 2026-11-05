using AStarDev.CloudSync.Infrastructure.OneDrive;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeMsalTokenCacheStorage : IMsalTokenCacheStorage
{
    private int secureStoreAttempts;
    private int plaintextFileAttempts;

    public bool SecureStoreFails { get; init; }

    public bool PlaintextFileFails { get; init; }

    public TimeSpan AttachDelay { get; init; } = TimeSpan.Zero;

    public int SecureStoreAttempts => secureStoreAttempts;

    public int PlaintextFileAttempts => plaintextFileAttempts;

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
}
