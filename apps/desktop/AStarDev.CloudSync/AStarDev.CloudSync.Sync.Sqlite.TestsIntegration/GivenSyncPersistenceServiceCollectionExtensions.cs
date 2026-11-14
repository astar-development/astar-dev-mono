using AStarDev.CloudSync.Accounts;
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

    [Fact]
    public void when_the_module_is_added_then_the_remote_index_reader_and_writer_are_registered_as_singletons_over_one_store()
    {
        var services = new ServiceCollection().AddSyncPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        services.Where(descriptor => descriptor.ServiceType == typeof(IRemoteIndexReader) || descriptor.ServiceType == typeof(IRemoteIndexWriter)).Select(descriptor => descriptor.Lifetime).ShouldBe([ServiceLifetime.Singleton, ServiceLifetime.Singleton]);
    }

    [Fact]
    public void when_the_module_is_added_then_the_activity_log_reader_and_writer_are_registered_as_singletons_over_one_store()
    {
        var services = new ServiceCollection().AddSyncPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        services.Where(descriptor => descriptor.ServiceType == typeof(IActivityLogReader) || descriptor.ServiceType == typeof(IActivityLogWriter)).Select(descriptor => descriptor.Lifetime).ShouldBe([ServiceLifetime.Singleton, ServiceLifetime.Singleton]);
        services.Count(descriptor => descriptor.ServiceType == typeof(IAccountRemovalParticipant)).ShouldBeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public void when_the_module_is_added_then_the_pause_reader_and_writer_are_registered_as_singletons_and_the_store_takes_part_in_account_removal()
    {
        var services = new ServiceCollection().AddSyncPersistence(_ => new MockFileSystem().FileInfo.New("/data/cloudsync.db"));

        services.Where(descriptor => descriptor.ServiceType == typeof(IAccountPauseReader) || descriptor.ServiceType == typeof(IAccountPauseWriter)).Select(descriptor => descriptor.Lifetime).ShouldBe([ServiceLifetime.Singleton, ServiceLifetime.Singleton]);
        services.Count(descriptor => descriptor.ServiceType == typeof(IAccountRemovalParticipant)).ShouldBeGreaterThanOrEqualTo(6);
    }
}
