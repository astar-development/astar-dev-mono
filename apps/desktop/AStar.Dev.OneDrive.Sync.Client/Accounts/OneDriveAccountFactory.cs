using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public static class OneDriveAccountFactory
{
    public static OneDriveAccount CreateFromWizardResult(string accountId, AccountProfile profile, IEnumerable<WizardFolderItem> selectedFolders)
    {
        var folders = selectedFolders.ToList();

        return new OneDriveAccount
        {
            Id               = new AccountId(accountId),
            Profile          = profile,
            SelectedFolderIds = [.. folders.Select(f => new OneDriveFolderId(f.Id))],
            FolderNames      = folders.ToDictionary(f => new OneDriveFolderId(f.Id), f => f.Name)
        };
    }
}
