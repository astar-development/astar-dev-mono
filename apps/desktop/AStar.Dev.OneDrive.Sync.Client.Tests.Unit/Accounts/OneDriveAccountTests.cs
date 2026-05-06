using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class OneDriveAccountTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var account = new OneDriveAccount();

        account.Profile.DisplayName.ShouldBe(string.Empty);
        account.Profile.Email.ShouldBe(string.Empty);
        account.AccentIndex.ShouldBe(0);
        account.SelectedFolderIds.ShouldBeEmpty();
        account.LastSyncedAt.ShouldBeNull();
        account.QuotaTotal.ShouldBe(0L);
        account.QuotaUsed.ShouldBe(0L);
        account.IsActive.ShouldBeFalse();
        account.FolderNames.ShouldBeEmpty();
        account.SyncConfig.ShouldBeNull();
    }

    [Fact]
    public void Id_WhenExplicitlySet_ShouldBePreserved()
    {
        var id = new AccountId("unique-account-id");
        var account = new OneDriveAccount { Id = id };

        account.Id.ShouldBe(id);
        account.Id.Id.ShouldBe("unique-account-id");
    }

    [Fact]
    public void DisplayName_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string displayName = "Jason Smith";

        account.Profile = AccountProfileFactory.Create(displayName, string.Empty);

        account.Profile.DisplayName.ShouldBe(displayName);
    }

    [Fact]
    public void Email_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string email = "jason@outlook.com";

        account.Profile = AccountProfileFactory.Create(string.Empty, email);

        account.Profile.Email.ShouldBe(email);
    }

    [Fact]
    public void AccentIndex_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        int accentIndex = 3;

        account.AccentIndex = accentIndex;

        account.AccentIndex.ShouldBe(accentIndex);
    }

    [Fact]
    public void SelectedFolderIds_ShouldBeModifiable()
    {
        var account = new OneDriveAccount();
        var folderId = new OneDriveFolderId("folder-123");

        account.SelectedFolderIds.Add(folderId);

        account.SelectedFolderIds.ShouldContain(folderId);
    }

    [Fact]
    public void LastSyncedAt_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        var lastSyncedAt = DateTimeOffset.UtcNow;

        account.LastSyncedAt = lastSyncedAt;

        account.LastSyncedAt.ShouldBe(lastSyncedAt);
    }

    [Fact]
    public void QuotaTotal_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        long quotaTotal = 1_099_511_627_776L;

        account.QuotaTotal = quotaTotal;

        account.QuotaTotal.ShouldBe(quotaTotal);
    }

    [Fact]
    public void QuotaUsed_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        long quotaUsed = 549_755_813_888L;

        account.QuotaUsed = quotaUsed;

        account.QuotaUsed.ShouldBe(quotaUsed);
    }

    [Fact]
    public void IsActive_ShouldBeSettable()
    {
        var account = new OneDriveAccount
        {
            IsActive = true
        };

        account.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void FolderNames_ShouldBeModifiable()
    {
        var account = new OneDriveAccount();
        var folderId = new OneDriveFolderId("folder-123");
        string folderName = "Documents";

        account.FolderNames[folderId] = folderName;

        account.FolderNames[folderId].ShouldBe(folderName);
    }

    [Fact]
    public void SyncConfig_ShouldBeSettableViaFactory()
    {
        var account = new OneDriveAccount();
        string rawPath = "/home/jason/OneDrive";
        var path = LocalSyncPathFactory.Create(rawPath).Match<LocalSyncPath?>(p => p, _ => null);

        account.SyncConfig = path is null ? null : AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, path);

        account.SyncConfig.ShouldNotBeNull();
        account.SyncConfig!.LocalSyncPath.Value.ShouldBe(rawPath);
    }

    [Fact]
    public void SyncConfig_ConflictPolicy_ShouldBeSettable()
    {
        var path = LocalSyncPath.Restore("/home/user/OneDrive");
        var account = new OneDriveAccount
        {
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, path)
        };

        account.SyncConfig!.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void SyncConfig_ShouldSupportMultiplePolicies(ConflictPolicy policy)
    {
        var path = LocalSyncPath.Restore("/home/user/OneDrive");
        var account = new OneDriveAccount
        {
            SyncConfig = AccountSyncConfigFactory.Create(policy, path)
        };

        account.SyncConfig!.ConflictPolicy.ShouldBe(policy);
    }

    [Fact]
    public void MultipleProperties_ShouldMaintainState()
    {
        var account = new OneDriveAccount();
        string displayName = "Jason Smith";
        string email = "jason@outlook.com";
        int accentIndex = 2;
        long quotaTotal = 1_099_511_627_776L;
        long quotaUsed = 549_755_813_888L;

        account.Profile = AccountProfileFactory.Create(displayName, email);
        account.AccentIndex = accentIndex;
        account.QuotaTotal = quotaTotal;
        account.QuotaUsed = quotaUsed;
        account.IsActive = true;

        account.Profile.DisplayName.ShouldBe(displayName);
        account.Profile.Email.ShouldBe(email);
        account.AccentIndex.ShouldBe(accentIndex);
        account.QuotaTotal.ShouldBe(quotaTotal);
        account.QuotaUsed.ShouldBe(quotaUsed);
        account.IsActive.ShouldBeTrue();
    }
}
