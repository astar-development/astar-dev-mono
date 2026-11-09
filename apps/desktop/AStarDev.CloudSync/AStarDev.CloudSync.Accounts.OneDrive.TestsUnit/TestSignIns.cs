using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public static class TestSignIns
{
    public const string AccountKey = "00000000-0000-0000-4a1b-2c3d4e5f6a7b.9188040d-6c67-4c5b-b112-36a304b66dad";

    public const string Email = "captain.flint@example.com";

    public const string DisplayName = "Captain Flint";

    public const string AccessToken = "eyJ0eXAi.secret-access-token.sig";

    public static readonly IReadOnlyList<string> GrantedScopes = ["User.Read", "Files.ReadWrite.All"];

    public static readonly AccountId AccountId = FakeAccountStore.AddedAccountId;

    public static MicrosoftSignIn NewAccount => MicrosoftSignInFactory.Create(AccountKey, Email, DisplayName, AccessToken, GrantedScopes);

    public static DriveId DriveId => DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message));

    public static StorageUsage Storage => StorageUsageFactory.Create(1_500, 5_000);
}
