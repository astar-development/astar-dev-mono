using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public class GivenAStoredAccountFactory
{
    [Fact]
    public void when_created_then_the_stored_account_keeps_its_id_and_registration()
    {
        var accountId = AccountIdFactory.Create(Guid.Parse("6f1c2b7e-3d4a-4e5f-8a9b-0c1d2e3f4a5b"));
        var registration = AccountRegistrationFactory.Create(new ProviderAccountKey("key"), CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("drive"), StorageUsageFactory.Create(0, 0));

        var storedAccount = StoredAccountFactory.Create(accountId, registration);

        (storedAccount.Id, storedAccount.Registration).ShouldBe((accountId, registration));
    }
}
