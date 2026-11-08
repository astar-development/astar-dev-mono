using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class FakeMsalClient : IMsalClient
{
    private readonly Lock gate = new();
    private readonly List<ProviderAccountKey> removedKeys = [];
    private readonly List<ProviderAccountKey> silentTokenKeys = [];
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

    public Func<ProviderAccountKey, CancellationToken, Task<Option<string>>> SilentToken { get; init; } = (_, _) => Task.FromResult(Option.Some(TestSignIns.AccessToken));

    public IReadOnlyList<ProviderAccountKey> SilentTokenKeys
    {
        get
        {
            lock (gate)
                return [.. silentTokenKeys];
        }
    }

    public Task<Option<string>> AcquireTokenSilentAsync(ProviderAccountKey key, CancellationToken cancellationToken)
    {
        lock (gate)
            silentTokenKeys.Add(key);

        return SilentToken(key, cancellationToken);
    }
}
