using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnOneDriveAccountFactory
{
    private const string AccountId   = "account-abc";
    private const string DisplayName = "Test User";
    private const string Email       = "test@example.com";
    private const string FolderId1   = "folder-1";
    private const string FolderName1 = "Documents";
    private const string FolderId2   = "folder-2";
    private const string FolderName2 = "Desktop";

    private static AccountProfile Profile => AccountProfileFactory.Create(DisplayName, Email);

    private static IEnumerable<WizardFolderItem> TwoSelectedFolders =>
    [
        new WizardFolderItem(FolderId1, FolderName1) { IsSelected = true },
        new WizardFolderItem(FolderId2, FolderName2) { IsSelected = true }
    ];

    [Fact]
    public void when_given_account_id_then_account_id_is_set()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, TwoSelectedFolders);

        result.Id.ShouldBe(new AccountId(AccountId));
    }

    [Fact]
    public void when_given_profile_then_profile_is_set()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, TwoSelectedFolders);

        result.Profile.DisplayName.ShouldBe(DisplayName);
        result.Profile.Email.ShouldBe(Email);
    }

    [Fact]
    public void when_given_selected_folders_then_selected_folder_ids_are_set()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, TwoSelectedFolders);

        result.SelectedFolderIds.Count.ShouldBe(2);
        result.SelectedFolderIds.ShouldContain(new OneDriveFolderId(FolderId1));
        result.SelectedFolderIds.ShouldContain(new OneDriveFolderId(FolderId2));
    }

    [Fact]
    public void when_given_selected_folders_then_folder_names_are_set()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, TwoSelectedFolders);

        result.FolderNames[new OneDriveFolderId(FolderId1)].ShouldBe(FolderName1);
        result.FolderNames[new OneDriveFolderId(FolderId2)].ShouldBe(FolderName2);
    }

    [Fact]
    public void when_given_no_selected_folders_then_selected_folder_ids_is_empty()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, []);

        result.SelectedFolderIds.ShouldBeEmpty();
    }

    [Fact]
    public void when_given_no_selected_folders_then_folder_names_is_empty()
    {
        var result = OneDriveAccountFactory.CreateFromWizardResult(AccountId, Profile, []);

        result.FolderNames.ShouldBeEmpty();
    }
}
