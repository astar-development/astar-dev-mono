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
}
