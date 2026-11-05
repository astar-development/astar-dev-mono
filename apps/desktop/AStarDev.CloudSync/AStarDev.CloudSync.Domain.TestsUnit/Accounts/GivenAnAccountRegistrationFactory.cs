using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.Domain.TestsUnit.Accounts;

public class GivenAnAccountRegistrationFactory
{
    private static readonly ProviderAccountKey AccountKey = new("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad");
    private static readonly DriveId PersonalDriveId = new("8A3F2C1D0E9B7A65");

    [Fact]
    public void when_created_then_the_registration_keeps_every_detail()
    {
        var storage = StorageUsageFactory.Create(1_024, 5_368_709_120);

        var registration = AccountRegistrationFactory.Create(AccountKey, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", PersonalDriveId, storage);

        registration.ShouldBe(new AccountRegistration(AccountKey, new CloudProvider.OneDrive(), "Personal", "personal@example.com", PersonalDriveId, storage));
    }

    [Fact]
    public void when_created_with_surrounding_whitespace_then_the_display_name_and_email_are_trimmed()
    {
        var registration = AccountRegistrationFactory.Create(AccountKey, CloudProviderFactory.CreateOneDrive(), "  Personal ", " personal@example.com\t", PersonalDriveId, StorageUsageFactory.Create(0, 0));

        (registration.DisplayName, registration.Email).ShouldBe(("Personal", "personal@example.com"));
    }

    [Fact]
    public void when_created_without_a_display_name_or_email_then_both_are_empty()
    {
        var registration = AccountRegistrationFactory.Create(AccountKey, CloudProviderFactory.CreateOneDrive(), null, null, PersonalDriveId, StorageUsageFactory.Create(0, 0));

        (registration.DisplayName, registration.Email).ShouldBe((string.Empty, string.Empty));
    }
}
