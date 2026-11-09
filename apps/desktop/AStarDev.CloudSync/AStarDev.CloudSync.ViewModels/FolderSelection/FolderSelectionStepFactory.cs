namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Creates <see cref="FolderSelectionStep"/> cases.</summary>
public static class FolderSelectionStepFactory
{
    /// <summary>Creates the closed step.</summary>
    public static FolderSelectionStep CreateClosed() => new FolderSelectionStep.Closed();

    /// <summary>Creates the choosing-account step.</summary>
    public static FolderSelectionStep CreateChoosingAccount() => new FolderSelectionStep.ChoosingAccount();

    /// <summary>Creates the tree step.</summary>
    public static FolderSelectionStep CreateTree() => new FolderSelectionStep.Tree();

    /// <summary>Creates the confirm-untick step.</summary>
    public static FolderSelectionStep CreateConfirmingUntick(int count) => new FolderSelectionStep.ConfirmingUntick(Math.Max(count, 0));

    /// <summary>Creates the saving step.</summary>
    public static FolderSelectionStep CreateSaving() => new FolderSelectionStep.Saving();
}
