using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenClassificationPersistenceServiceCollectionExtensions
{
    private static ServiceCollection Added() => (ServiceCollection)new ServiceCollection().AddClassificationPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

    [Fact]
    public void when_the_module_is_added_then_the_same_collection_is_returned_for_chaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddClassificationPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        returned.ShouldBeSameAs(services);
    }

    [Fact]
    public void when_the_module_is_added_then_every_port_is_registered_as_a_singleton()
    {
        var services = Added();

        new[] { typeof(ITermStore), typeof(ICategoryStore), typeof(IFileClassificationStore), typeof(ISeedStateStore), typeof(IClassifiableFileSource), typeof(ICloudSyncDatabase) }
            .Select(port => services.Single(descriptor => descriptor.ServiceType == port).Lifetime)
            .ShouldAllBe(lifetime => lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void when_the_module_is_added_after_another_database_then_one_database_registration_remains()
    {
        var services = new ServiceCollection().AddSingleton<ICloudSyncDatabase>(_ => throw new InvalidOperationException("Not built in this test."));

        _ = services.AddClassificationPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        services.Count(descriptor => descriptor.ServiceType == typeof(ICloudSyncDatabase)).ShouldBe(1);
    }
}
