using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnOneDriveAccount
{
    [Fact]
    public void when_constructed_then_all_properties_have_default_values()
    {
        var account = new OneDriveAccount();

        account.Profile.DisplayName.ShouldBe(string.Empty);
        account.Profile.Email.ShouldBe(string.Empty);
        account.AccentIndex.ShouldBe(0);
        account.SelectedFolderIds.ShouldBeEmpty();
        account.LastSyncedAt.ShouldBeNull();
        account.Quota.TotalBytes.ShouldBe(0L);
        account.Quota.UsedBytes.ShouldBe(0L);
        account.IsActive.ShouldBeFalse();
        account.FolderNames.ShouldBeEmpty();
        account.SyncConfig.ShouldBeNull();
    }

    [Fact]
    public void when_id_is_set_then_it_is_preserved()
    {
        var id = new AccountId("unique-account-id");
        var account = new OneDriveAccount { Id = id };

        account.Id.ShouldBe(id);
        account.Id.Id.ShouldBe("unique-account-id");
    }

    [Fact]
    public void when_profile_display_name_is_set_then_it_is_preserved()
    {
        var account = new OneDriveAccount();
        string displayName = "Jason Smith";

        account.Profile = AccountProfileFactory.Create(displayName, string.Empty);

        account.Profile.DisplayName.ShouldBe(displayName);
    }

    [Fact]
    public void when_profile_email_is_set_then_it_is_preserved()
    {
        var account = new OneDriveAccount();
        string email = "jason@outlook.com";

        account.Profile = AccountProfileFactory.Create(string.Empty, email);

        account.Profile.Email.ShouldBe(email);
    }

    [Fact]
    public void when_accent_index_is_set_then_it_is_preserved()
    {
        var account = new OneDriveAccount();
        int accentIndex = 3;

        account.AccentIndex = accentIndex;

        account.AccentIndex.ShouldBe(accentIndex);
    }

    [Fact]
    public void when_folder_id_is_added_then_it_is_in_selected_folder_ids()
    {
        var account = new OneDriveAccount();
        var folderId = new OneDriveFolderId("folder-123");

        account.SelectedFolderIds.Add(folderId);

        account.SelectedFolderIds.ShouldContain(folderId);
    }

    [Fact]
    public void when_last_synced_at_is_set_then_it_is_preserved()
    {
        var account = new OneDriveAccount();
        var lastSyncedAt = DateTimeOffset.UtcNow;

        account.LastSyncedAt = lastSyncedAt;

        account.LastSyncedAt.ShouldBe(lastSyncedAt);
    }

    [Fact]
    public void when_quota_is_set_then_total_bytes_is_preserved()
    {
        var account = new OneDriveAccount();
        long totalBytes = 1_099_511_627_776L;

        account.Quota = StorageQuotaFactory.Create(totalBytes, 0L);

        account.Quota.TotalBytes.ShouldBe(totalBytes);
    }

    [Fact]
    public void when_quota_is_set_then_used_bytes_is_preserved()
    {
        var account = new OneDriveAccount();
        long usedBytes = 549_755_813_888L;

        account.Quota = StorageQuotaFactory.Create(1_099_511_627_776L, usedBytes);

        account.Quota.UsedBytes.ShouldBe(usedBytes);
    }

    [Fact]
    public void when_is_active_is_set_to_true_then_it_is_true()
    {
        var account = new OneDriveAccount
        {
            IsActive = true
        };

        account.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void when_folder_name_is_set_then_it_is_in_folder_names()
    {
        var account = new OneDriveAccount();
        var folderId = new OneDriveFolderId("folder-123");
        string folderName = "Documents";

        account.FolderNames[folderId] = folderName;

        account.FolderNames[folderId].ShouldBe(folderName);
    }

    [Fact]
    public void when_sync_config_is_set_via_factory_then_local_sync_path_is_preserved()
    {
        var account = new OneDriveAccount();
        string rawPath = "/home/jason/OneDrive";
        var path = LocalSyncPathFactory.Create(rawPath).Match<LocalSyncPath?>(p => p, _ => null);

        account.SyncConfig = path is null ? null : AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, path);

        account.SyncConfig.ShouldNotBeNull();
        account.SyncConfig!.LocalSyncPath.Value.ShouldBe(rawPath);
    }

    [Fact]
    public void when_sync_config_is_created_with_conflict_policy_then_it_is_preserved()
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
    public void when_sync_config_is_created_with_any_conflict_policy_then_it_is_preserved(ConflictPolicy policy)
    {
        var path = LocalSyncPath.Restore("/home/user/OneDrive");
        var account = new OneDriveAccount
        {
            SyncConfig = AccountSyncConfigFactory.Create(policy, path)
        };

        account.SyncConfig!.ConflictPolicy.ShouldBe(policy);
    }

    [Fact]
    public void when_multiple_properties_are_set_then_they_are_all_maintained()
    {
        var account = new OneDriveAccount();
        string displayName = "Jason Smith";
        string email = "jason@outlook.com";
        int accentIndex = 2;
        long quotaTotal = 1_099_511_627_776L;
        long quotaUsed = 549_755_813_888L;

        account.Profile = AccountProfileFactory.Create(displayName, email);
        account.AccentIndex = accentIndex;
        account.Quota = StorageQuotaFactory.Create(quotaTotal, quotaUsed);
        account.IsActive = true;

        account.Profile.DisplayName.ShouldBe(displayName);
        account.Profile.Email.ShouldBe(email);
        account.AccentIndex.ShouldBe(accentIndex);
        account.Quota.TotalBytes.ShouldBe(quotaTotal);
        account.Quota.UsedBytes.ShouldBe(quotaUsed);
        account.IsActive.ShouldBeTrue();
    }
}
