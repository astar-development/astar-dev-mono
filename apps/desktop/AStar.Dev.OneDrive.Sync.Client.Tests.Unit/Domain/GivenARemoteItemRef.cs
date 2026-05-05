using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenARemoteItemRef
{
    private const string AccountId = "account-123";
    private const string FolderId = "folder-456";
    private const string RemoteItemId = "item-789";

    [Fact]
    public void when_created_then_account_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);

        remoteItemRef.AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_created_then_folder_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);

        remoteItemRef.FolderId.ShouldBe(FolderId);
    }

    [Fact]
    public void when_created_then_remote_item_id_is_set_correctly()
    {
        var remoteItemRef = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);

        remoteItemRef.RemoteItemId.ShouldBe(RemoteItemId);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);
        var second = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_account_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);
        var second = RemoteItemRefFactory.Create("account-different", FolderId, RemoteItemId);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_folder_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);
        var second = RemoteItemRefFactory.Create(AccountId, "folder-different", RemoteItemId);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_remote_item_ids_then_they_are_not_equal()
    {
        var first = RemoteItemRefFactory.Create(AccountId, FolderId, RemoteItemId);
        var second = RemoteItemRefFactory.Create(AccountId, FolderId, "item-different");

        first.ShouldNotBe(second);
    }
}
