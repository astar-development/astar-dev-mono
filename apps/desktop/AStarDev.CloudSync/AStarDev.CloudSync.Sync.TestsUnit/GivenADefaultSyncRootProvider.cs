using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADefaultSyncRootProvider
{
    private const string Home = "/home/jbarden";
    private static readonly AccountId OtherAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    [Fact]
    public void when_nothing_clashes_then_the_default_is_the_provider_and_display_name_under_cloud_sync_in_the_home_folder()
    {
        var result = Provider().CreateFor(Registration("Personal"), []);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/home/jbarden/CloudSync/OneDrive-Personal");
    }

    [Theory]
    [InlineData("Work/Team", "OneDrive-Work_Team")]
    [InlineData("Ann: A*B?", "OneDrive-Ann_ A_B_")]
    [InlineData("  Spaced  ", "OneDrive-Spaced")]
    [InlineData("Dots..", "OneDrive-Dots")]
    [InlineData("", "OneDrive-Account")]
    [InlineData("   ", "OneDrive-Account")]
    public void when_the_display_name_is_not_safe_for_a_folder_name_then_it_is_made_safe(string displayName, string expectedFolder)
    {
        var result = Provider().CreateFor(Registration(displayName), []);

        result.Match(root => (object)root.Directory.Name, rejection => rejection).ShouldBe(expectedFolder);
    }

    [Fact]
    public void when_the_default_is_the_same_as_an_existing_root_then_a_numeric_suffix_is_added()
    {
        var existing = new[] { Existing("/home/jbarden/cloudsync/onedrive-personal/") };

        var result = Provider().CreateFor(Registration("Personal"), existing);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/home/jbarden/CloudSync/OneDrive-Personal-2");
    }

    [Fact]
    public void when_several_defaults_are_taken_then_the_first_free_suffix_is_used()
    {
        var existing = new[] { Existing("/home/jbarden/CloudSync/OneDrive-Personal"), Existing("/home/jbarden/CloudSync/OneDrive-Personal-2"), Existing("/home/jbarden/CloudSync/OneDrive-Personal-3") };

        var result = Provider().CreateFor(Registration("Personal"), existing);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/home/jbarden/CloudSync/OneDrive-Personal-4");
    }

    [Fact]
    public void when_an_existing_root_contains_every_default_then_no_root_is_available()
    {
        var existing = new[] { Existing("/home/jbarden/CloudSync") };

        var result = Provider().CreateFor(Registration("Personal"), existing);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateUnavailable());
    }

    [Fact]
    public void when_there_is_no_home_folder_then_no_root_is_available()
    {
        var provider = new DefaultSyncRootProvider(new TestHomeFolder(Option.None<System.IO.Abstractions.IDirectoryInfo>()), new SyncRootPolicy());

        var result = provider.CreateFor(Registration("Personal"), []);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateUnavailable());
    }

    private DefaultSyncRootProvider Provider() => new(new TestHomeFolder(Option.Some(fileSystem.DirectoryInfo.New(Home))), new SyncRootPolicy());

    private AccountSyncRoot Existing(string path) =>
        AccountSyncRootFactory.Create(OtherAccount, SyncRootFactory.Create(fileSystem.DirectoryInfo.New(path)).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid.")));

    private static AccountRegistration Registration(string displayName) =>
        AccountRegistrationFactory.Create(
            ProviderAccountKeyFactory.Create("key-1").Match(key => key, _ => throw new InvalidOperationException("The test key must be valid.")),
            CloudProviderFactory.CreateOneDrive(),
            displayName,
            "personal@example.com",
            new DriveId("8A3F2C1D0E9B7A65"),
            StorageUsageFactory.Create(1, 2));
}
