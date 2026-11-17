using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal static class AccountSyncLocks
{
    internal static IAccountSyncLock Create()
    {
        using var provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();

        return provider.GetRequiredService<IAccountSyncLock>();
    }
}
