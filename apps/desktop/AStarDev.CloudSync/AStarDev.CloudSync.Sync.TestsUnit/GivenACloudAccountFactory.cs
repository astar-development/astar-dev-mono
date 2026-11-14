using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenACloudAccountFactory
{
    [Fact]
    public void when_the_int_counts_are_negative_then_they_are_normalised_to_zero()
    {
        var account = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), -3, -4, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());

        (Shown(account.FolderCount), Shown(account.FileCount)).ShouldBe(("0", "0"));
    }

    [Fact]
    public void when_the_option_counts_are_negative_then_they_are_normalised_to_zero()
    {
        var account = Create(Option.Some(-3), Option.Some(-4));

        (Shown(account.FolderCount), Shown(account.FileCount)).ShouldBe(("0", "0"));
    }

    [Fact]
    public void when_the_option_counts_are_none_then_they_stay_unknown()
    {
        var account = Create(Option.None<int>(), Option.Some(5));

        (Shown(account.FolderCount), Shown(account.FileCount)).ShouldBe(("unknown", "5"));
    }

    private static CloudAccount Create(Option<int> folders, Option<int> files) => CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), folders, files, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), AccountSignInStateFactory.CreateSignedIn());

    private static string Shown(Option<int> count) => count.Match(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture), () => "unknown");
}
