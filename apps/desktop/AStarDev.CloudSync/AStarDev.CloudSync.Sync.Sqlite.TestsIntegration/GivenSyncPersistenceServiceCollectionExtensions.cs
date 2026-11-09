using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public class GivenSyncPersistenceServiceCollectionExtensions
{
    [Fact]
    public void when_the_module_is_added_then_the_same_collection_is_returned_for_chaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddSyncPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        returned.ShouldBeSameAs(services);
    }
}
