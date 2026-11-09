using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class AccountFixtures
{
    public const string Email = "jane.doe@example.com";

    public static AccountRegistration Registration(string identifier) => AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create(identifier).Match(key => key, _ => throw new InvalidOperationException("The test key must be valid.")), CloudProviderFactory.CreateOneDrive(), "Jane Doe", Email, new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1_024, 5_368_709_120));
}
