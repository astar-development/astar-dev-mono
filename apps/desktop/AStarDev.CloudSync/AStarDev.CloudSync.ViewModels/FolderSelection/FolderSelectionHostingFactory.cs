namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Creates <see cref="FolderSelectionHosting"/> cases.</summary>
public static class FolderSelectionHostingFactory
{
    /// <summary>Creates the dialog hosting.</summary>
    public static FolderSelectionHosting CreateDialog() => new FolderSelectionHosting.Dialog();

    /// <summary>Creates the page hosting.</summary>
    public static FolderSelectionHosting CreatePage() => new FolderSelectionHosting.Page();
}
