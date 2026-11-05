using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeMsalClient : IMsalClient
{
    private readonly List<ProviderAccountKey> removedKeys = [];

    public IReadOnlyList<string> CachedAccountKeys { get; init; } = [];

    public Func<CancellationToken, Task<MicrosoftSignIn>> SignIn { get; init; } = _ => Task.FromResult(TestSignIns.NewAccount);

    public int SignInAttempts { get; private set; }

    public IReadOnlyList<ProviderAccountKey> RemovedKeys => removedKeys;

    public Task<IReadOnlyList<string>> GetAccountKeysAsync() => Task.FromResult(CachedAccountKeys);

    public Task<MicrosoftSignIn> SignInAsync(CancellationToken cancellationToken)
    {
        SignInAttempts++;

        return SignIn(cancellationToken);
    }

    public Task RemoveAccountAsync(ProviderAccountKey key)
    {
        removedKeys.Add(key);

        return Task.CompletedTask;
    }
}
