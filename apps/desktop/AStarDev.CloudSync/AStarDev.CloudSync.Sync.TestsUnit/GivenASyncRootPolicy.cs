using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncRootPolicy
{
    private const string Personal = "/home/jbarden/CloudSync/OneDrive-Personal";
    private const string OneDrive = "/home/jbarden/CloudSync/OneDrive";
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    [Theory]
    [InlineData(Personal, "/home/jbarden/CloudSync/OneDrive-Work")]
    [InlineData(OneDrive, "/home/jbarden/CloudSync/OneDrive-Work")]
    public void when_the_candidate_is_a_sibling_then_it_is_allowed(string existingRoot, string candidate)
    {
        var result = Check(SecondAccount, candidate, (FirstAccount, existingRoot));

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe(candidate);
    }

    [Theory]
    [InlineData(OneDrive, "/home/jbarden/CloudSync/OneDrive")]
    [InlineData(OneDrive, "/home/jbarden/cloudsync/onedrive/")]
    [InlineData(OneDrive, "/HOME/JBARDEN/CLOUDSYNC/ONEDRIVE//")]
    public void when_the_candidate_is_the_same_folder_then_it_is_rejected_as_the_same_as_the_other_account(string existingRoot, string candidate)
    {
        var result = Check(SecondAccount, candidate, (FirstAccount, existingRoot));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSameAs(FirstAccount));
    }

    [Theory]
    [InlineData(OneDrive, "/home/jbarden/CloudSync/OneDrive/Work")]
    [InlineData(OneDrive, "/home/jbarden/cloudsync/ONEDRIVE/work/")]
    public void when_the_candidate_is_inside_another_accounts_root_then_it_is_rejected_as_inside_of_it(string existingRoot, string candidate)
    {
        var result = Check(SecondAccount, candidate, (FirstAccount, existingRoot));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateInsideOf(FirstAccount));
    }

    [Theory]
    [InlineData("/home/jbarden/CloudSync/OneDrive/Work", OneDrive)]
    [InlineData("/home/jbarden/CloudSync/OneDrive/Work", "/home/jbarden/cloudsync/onedrive/")]
    [InlineData("/home/jbarden/CloudSync/OneDrive", "/")]
    public void when_the_candidate_contains_another_accounts_root_then_it_is_rejected_as_containing_it(string existingRoot, string candidate)
    {
        var result = Check(SecondAccount, candidate, (FirstAccount, existingRoot));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateContains(FirstAccount));
    }

    [Theory]
    [InlineData(OneDrive)]
    [InlineData("/home/jbarden/cloudsync/onedrive/")]
    [InlineData("/home/jbarden/CloudSync/OneDrive/Work")]
    [InlineData("/home/jbarden/CloudSync")]
    public void when_the_candidate_is_the_accounts_own_root_or_changes_it_then_the_own_root_is_ignored(string candidate)
    {
        var result = Check(FirstAccount, candidate, (FirstAccount, OneDrive));

        result.Match(root => true, rejection => false).ShouldBeTrue();
    }

    [Fact]
    public void when_the_candidate_clashes_with_one_of_several_accounts_then_that_account_is_named()
    {
        var third = AccountIdFactory.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var result = Check(third, "/home/jbarden/CloudSync/OneDrive-Work/Photos", (FirstAccount, Personal), (SecondAccount, "/home/jbarden/CloudSync/OneDrive-Work"));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateInsideOf(SecondAccount));
    }

    [Fact]
    public void when_the_candidate_is_a_symlink_to_another_accounts_root_then_it_is_rejected_as_the_same_as_that_account()
    {
        _ = fileSystem.Directory.CreateDirectory(OneDrive);
        _ = fileSystem.Directory.CreateSymbolicLink("/home/jbarden/link-to-onedrive", OneDrive);

        var result = Check(SecondAccount, "/home/jbarden/link-to-onedrive", (FirstAccount, OneDrive));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSameAs(FirstAccount));
    }

    [Fact]
    public void when_another_accounts_root_is_a_symlink_to_the_candidate_then_it_is_rejected_as_the_same_as_that_account()
    {
        _ = fileSystem.Directory.CreateDirectory("/mnt/data/sync");
        _ = fileSystem.Directory.CreateSymbolicLink(OneDrive, "/mnt/data/sync");

        var result = Check(SecondAccount, "/mnt/data/sync", (FirstAccount, OneDrive));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSameAs(FirstAccount));
    }

    [Fact]
    public void when_there_is_no_candidate_then_it_is_rejected_as_not_absolute()
    {
        var result = new SyncRootPolicy().Check(SecondAccount, null!, []);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateNotAbsolute());
    }

    [Fact]
    public void when_a_root_is_created_with_a_trailing_separator_then_the_separator_is_removed()
    {
        var result = SyncRootFactory.Create(fileSystem.DirectoryInfo.New("/home/jbarden/CloudSync/OneDrive/"));

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe(OneDrive);
    }

    private Result<SyncRoot, SyncRootRejection> Check(AccountId accountId, string candidate, params (AccountId Account, string Root)[] existing) =>
        new SyncRootPolicy().Check(accountId, fileSystem.DirectoryInfo.New(candidate), [.. existing.Select(entry => AccountSyncRootFactory.Create(entry.Account, Root(entry.Root)))]);

    private SyncRoot Root(string path) => SyncRootFactory.Create(fileSystem.DirectoryInfo.New(path)).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid."));
}
