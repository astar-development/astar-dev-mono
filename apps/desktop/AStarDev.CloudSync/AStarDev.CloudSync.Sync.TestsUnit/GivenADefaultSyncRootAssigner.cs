using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADefaultSyncRootAssigner
{
    private static readonly AccountId NewAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_no_root_clashes_then_the_default_root_is_saved_for_the_account()
    {
        var store = new InMemorySyncRootStore(new SyncRootPolicy());

        var result = await Assigner(store).AssignAsync(NewAccount, Registration(), CancellationToken);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/home/jbarden/CloudSync/OneDrive-Personal");
        store.Roots.Select(entry => (entry.AccountId, entry.Root.Directory.FullName)).ShouldContain((NewAccount, "/home/jbarden/CloudSync/OneDrive-Personal"));
    }

    [Fact]
    public async Task when_another_account_takes_the_proposed_root_first_then_the_next_suffix_is_proposed()
    {
        var store = new InMemorySyncRootStore(new SyncRootPolicy()) { LostRaces = 1 };

        var result = await Assigner(store).AssignAsync(NewAccount, Registration(), CancellationToken);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/home/jbarden/CloudSync/OneDrive-Personal-2");
        store.SetAttempts.ShouldBe(2);
    }

    [Fact]
    public async Task when_every_attempt_loses_the_race_then_the_last_rejection_is_returned()
    {
        var store = new InMemorySyncRootStore(new SyncRootPolicy()) { LostRaces = 100 };

        var result = await Assigner(store).AssignAsync(NewAccount, Registration(), CancellationToken);

        result.Match(root => false, rejection => rejection is SyncRootRejection.SameAs).ShouldBeTrue();
        store.SetAttempts.ShouldBe(5);
    }

    [Fact]
    public async Task when_the_roots_cannot_be_read_then_a_store_rejection_is_returned_and_nothing_is_saved()
    {
        var store = new InMemorySyncRootStore(new SyncRootPolicy()) { ReadFails = Option.Some<Exception>(new InvalidOperationException()) };

        var result = await Assigner(store).AssignAsync(NewAccount, Registration(), CancellationToken);

        result.Match(root => false, rejection => rejection is SyncRootRejection.Store).ShouldBeTrue();
        store.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_there_is_no_home_folder_then_no_root_is_assigned_and_nothing_is_saved()
    {
        var store = new InMemorySyncRootStore(new SyncRootPolicy());
        var provider = new DefaultSyncRootProvider(new TestHomeFolder(Option.None<System.IO.Abstractions.IDirectoryInfo>()), new SyncRootPolicy());

        var result = await new DefaultSyncRootAssigner(store, provider, store).AssignAsync(NewAccount, Registration(), CancellationToken);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateUnavailable());
        store.SetAttempts.ShouldBe(0);
    }

    private DefaultSyncRootAssigner Assigner(InMemorySyncRootStore store) =>
        new(store, new DefaultSyncRootProvider(new TestHomeFolder(Option.Some(fileSystem.DirectoryInfo.New("/home/jbarden"))), new SyncRootPolicy()), store);

    private static AccountRegistration Registration() =>
        AccountRegistrationFactory.Create(
            ProviderAccountKeyFactory.Create("key-1").Match(key => key, _ => throw new InvalidOperationException("The test key must be valid.")),
            CloudProviderFactory.CreateOneDrive(),
            "Personal",
            "personal@example.com",
            new DriveId("8A3F2C1D0E9B7A65"),
            StorageUsageFactory.Create(1, 2));
}
