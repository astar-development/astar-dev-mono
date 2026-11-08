using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class FakeMsalClient : IMsalClient
{
    private readonly Lock gate = new();
    private readonly List<ProviderAccountKey> removedKeys = [];
    private int signInAttempts;

    public IReadOnlyList<string> CachedAccountKeys { get; init; } = [];

    public Func<CancellationToken, Task<MicrosoftSignIn>> SignIn { get; init; } = _ => Task.FromResult(TestSignIns.NewAccount);

    public int SignInAttempts => signInAttempts;

    public IReadOnlyList<ProviderAccountKey> RemovedKeys
    {
        get
        {
            lock (gate)
                return [.. removedKeys];
        }
    }

    public Task<IReadOnlyList<string>> GetAccountKeysAsync() => Task.FromResult(CachedAccountKeys);

    public Task<MicrosoftSignIn> SignInAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref signInAttempts);

        return SignIn(cancellationToken);
    }

    public Task RemoveAccountAsync(ProviderAccountKey key)
    {
        lock (gate)
            removedKeys.Add(key);

        return Task.CompletedTask;
    }
}
