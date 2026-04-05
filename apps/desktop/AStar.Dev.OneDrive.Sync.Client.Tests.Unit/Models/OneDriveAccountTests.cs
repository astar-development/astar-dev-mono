using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

public class OneDriveAccountTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var account = new OneDriveAccount();

        account.Id.ShouldNotBeNullOrEmpty();
        account.DisplayName.ShouldBe(string.Empty);
        account.Email.ShouldBe(string.Empty);
        account.AccentIndex.ShouldBe(0);
        account.SelectedFolderIds.ShouldBeEmpty();
        account.DeltaLink.ShouldBeNull();
        account.LastSyncedAt.ShouldBeNull();
        account.QuotaTotal.ShouldBe(0L);
        account.QuotaUsed.ShouldBe(0L);
        account.IsActive.ShouldBeFalse();
        account.FolderNames.ShouldBeEmpty();
        account.LocalSyncPath.ShouldBe(string.Empty);
        account.ConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        var account1 = new OneDriveAccount();
        var account2 = new OneDriveAccount();

        account1.Id.ShouldNotBe(account2.Id);
    }

    [Fact]
    public void DisplayName_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string displayName = "Jason Smith";

        account.DisplayName = displayName;

        account.DisplayName.ShouldBe(displayName);
    }

    [Fact]
    public void Email_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string email = "jason@outlook.com";

        account.Email = email;

        account.Email.ShouldBe(email);
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
        string folderId = "folder-123";

        account.SelectedFolderIds.Add(folderId);

        account.SelectedFolderIds.ShouldContain(folderId);
    }

    [Fact]
    public void DeltaLink_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string deltaLink = "https://graph.microsoft.com/v1.0/drives/abc123/root/delta?token=abc";

        account.DeltaLink = deltaLink;

        account.DeltaLink.ShouldBe(deltaLink);
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
        long quotaTotal = 1_099_511_627_776L; // 1 TB

        account.QuotaTotal = quotaTotal;

        account.QuotaTotal.ShouldBe(quotaTotal);
    }

    [Fact]
    public void QuotaUsed_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        long quotaUsed = 549_755_813_888L; // 512 GB

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
        string folderId = "folder-123";
        string folderName = "Documents";

        account.FolderNames[folderId] = folderName;

        account.FolderNames[folderId].ShouldBe(folderName);
    }

    [Fact]
    public void LocalSyncPath_ShouldBeSettable()
    {
        var account = new OneDriveAccount();
        string localSyncPath = "/home/jason/OneDrive";

        account.LocalSyncPath = localSyncPath;

        account.LocalSyncPath.ShouldBe(localSyncPath);
    }

    [Fact]
    public void ConflictPolicy_ShouldBeSettable()
    {
        var account = new OneDriveAccount
        {
            ConflictPolicy = ConflictPolicy.LastWriteWins
        };

        account.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void ConflictPolicy_ShouldSupportMultiplePolicies(ConflictPolicy policy)
    {
        var account = new OneDriveAccount
        {
            ConflictPolicy = policy
        };

        account.ConflictPolicy.ShouldBe(policy);
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

        account.DisplayName = displayName;
        account.Email = email;
        account.AccentIndex = accentIndex;
        account.QuotaTotal = quotaTotal;
        account.QuotaUsed = quotaUsed;
        account.IsActive = true;

        account.DisplayName.ShouldBe(displayName);
        account.Email.ShouldBe(email);
        account.AccentIndex.ShouldBe(accentIndex);
        account.QuotaTotal.ShouldBe(quotaTotal);
        account.QuotaUsed.ShouldBe(quotaUsed);
        account.IsActive.ShouldBeTrue();
    }
}
