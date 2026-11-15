namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Creates <see cref="LayoutAPage"/> cases.</summary>
public static class LayoutAPageFactory
{
    /// <summary>Creates the accounts page.</summary>
    public static LayoutAPage CreateAccounts() => new LayoutAPage.Accounts();

    /// <summary>Creates the folders page.</summary>
    public static LayoutAPage CreateFolders() => new LayoutAPage.Folders();

    /// <summary>Creates the conflicts page.</summary>
    public static LayoutAPage CreateConflicts() => new LayoutAPage.Conflicts();
}
