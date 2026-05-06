using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenARemoteItemRef
{
    private const string AccountIdValue = "account-123";
    private const string FolderIdValue = "folder-456";
    private const string RemoteItemIdValue = "item-789";

    [Fact]
    public void when_created_then_account_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));

        remoteItemRef.AccountId.Id.ShouldBe(AccountIdValue);
    }

    [Fact]
    public void when_created_then_folder_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));

        remoteItemRef.FolderId.Id.ShouldBe(FolderIdValue);
    }

    [Fact]
    public void when_created_then_remote_item_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));

        remoteItemRef.RemoteItemId.Id.ShouldBe(RemoteItemIdValue);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));
        var second = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_account_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));
        var second = RemoteItemRefFactory.Create(new AccountId("account-different"), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_folder_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));
        var second = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId("folder-different"), new OneDriveItemId(RemoteItemIdValue));

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_remote_item_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId(RemoteItemIdValue));
        var second = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(FolderIdValue), new OneDriveItemId("item-different"));

        first.ShouldNotBe(second);
    }
}
