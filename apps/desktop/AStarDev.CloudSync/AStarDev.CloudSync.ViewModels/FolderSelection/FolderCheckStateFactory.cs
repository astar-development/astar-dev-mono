namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Creates <see cref="FolderCheckState"/> cases.</summary>
public static class FolderCheckStateFactory
{
    /// <summary>Creates the <see cref="FolderCheckState.Ticked"/> case.</summary>
    public static FolderCheckState CreateTicked() => new FolderCheckState.Ticked();

    /// <summary>Creates the <see cref="FolderCheckState.Unticked"/> case.</summary>
    public static FolderCheckState CreateUnticked() => new FolderCheckState.Unticked();

    /// <summary>Creates the <see cref="FolderCheckState.PartiallyTicked"/> case.</summary>
    public static FolderCheckState CreatePartiallyTicked() => new FolderCheckState.PartiallyTicked();
}
