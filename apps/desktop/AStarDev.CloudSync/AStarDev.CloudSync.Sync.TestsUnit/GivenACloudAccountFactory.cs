using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenACloudAccountFactory
{
    [Fact]
    public void when_the_int_counts_are_negative_then_they_are_normalised_to_zero()
    {
        var account = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), -3, -4, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());

        (Shown(account.FolderCount), Shown(account.FileCount)).ShouldBe(("0", "0"));
    }

    [Fact]
    public void when_the_text_is_missing_then_it_is_normalised_to_empty()
    {
        var account = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), null!, null!, CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), 1, 2, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());

        (account.DisplayName, account.Email).ShouldBe((string.Empty, string.Empty));
    }

    [Fact]
    public void when_no_sign_in_state_is_given_then_the_account_is_signed_in()
    {
        var account = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), 1, 2, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());

        account.SignIn.ShouldBe(AccountSignInStateFactory.CreateSignedIn());
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

    private static CloudAccount Create(Option<int> folders, Option<int> files) => CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), folders, files, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), AccountSignInStateFactory.CreateSignedIn());

    private static string Shown(Option<int> count) => count.Match(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture), () => "unknown");
}
