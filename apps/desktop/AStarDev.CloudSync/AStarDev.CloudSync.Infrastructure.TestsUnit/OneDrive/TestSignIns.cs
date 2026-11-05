using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public static class TestSignIns
{
    public const string AccountKey = "00000000-0000-0000-4a1b-2c3d4e5f6a7b.9188040d-6c67-4c5b-b112-36a304b66dad";

    public const string Email = "captain.flint@example.com";

    public const string DisplayName = "Captain Flint";

    public const string AccessToken = "eyJ0eXAi.secret-access-token.sig";

    public static readonly IReadOnlyList<string> GrantedScopes = ["User.Read", "Files.ReadWrite.All"];

    public static readonly AccountId AccountId = AccountIdFactory.Create(Guid.Parse("2b9f1c0e-5d43-4a7e-9e61-0c8a7b6d5e4f"));

    public static MicrosoftSignIn NewAccount => MicrosoftSignInFactory.Create(AccountKey, Email, DisplayName, AccessToken, GrantedScopes);

    public static MicrosoftDrive Drive => MicrosoftDriveFactory.Create(DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message)), StorageUsageFactory.Create(1_500, 5_000));
}
