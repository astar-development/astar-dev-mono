namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Creates folder selections for hosting inside a page, independent of the modal dialog and of each other.</summary>
public interface IFolderSelectionFactory
{
    /// <summary>Creates a page-hosted folder selection that shows nothing until its show-account command runs.</summary>
    /// <returns>The new folder selection.</returns>
    FolderSelectionViewModel CreatePage();
}
