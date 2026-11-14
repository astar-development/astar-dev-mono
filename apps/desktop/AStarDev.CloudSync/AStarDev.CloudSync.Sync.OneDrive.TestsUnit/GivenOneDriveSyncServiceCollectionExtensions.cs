using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public class GivenOneDriveSyncServiceCollectionExtensions
{
    [Fact]
    public void when_the_module_is_added_then_the_same_collection_is_returned_for_chaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddOneDriveSync();

        returned.ShouldBeSameAs(services);
    }

    [Fact]
    public void when_the_module_is_added_then_the_folder_browser_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IRemoteFolderBrowser) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void when_the_module_is_added_then_the_item_renamer_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IRemoteItemRenamer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void when_the_module_is_added_then_the_change_feed_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IRemoteChangeFeed) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}
