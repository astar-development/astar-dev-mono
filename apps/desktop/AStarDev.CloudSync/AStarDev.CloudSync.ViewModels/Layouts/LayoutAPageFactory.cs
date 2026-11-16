namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Creates <see cref="LayoutAPage"/> cases.</summary>
public static class LayoutAPageFactory
{
    /// <summary>Creates the accounts page.</summary>
    public static LayoutAPage CreateAccounts() => new LayoutAPage.Accounts();

    /// <summary>Creates the search page.</summary>
    public static LayoutAPage CreateSearch() => new LayoutAPage.Search();

    /// <summary>Creates the activity page.</summary>
    public static LayoutAPage CreateActivity() => new LayoutAPage.Activity();

    /// <summary>Creates the folders page.</summary>
    public static LayoutAPage CreateFolders() => new LayoutAPage.Folders();

    /// <summary>Creates the conflicts page.</summary>
    public static LayoutAPage CreateConflicts() => new LayoutAPage.Conflicts();

    /// <summary>Creates the classification page.</summary>
    public static LayoutAPage CreateClassification() => new LayoutAPage.Classification();
}
