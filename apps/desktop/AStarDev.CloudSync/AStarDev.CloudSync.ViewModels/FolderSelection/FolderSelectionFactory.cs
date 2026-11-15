using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Creates a new page-hosted <see cref="FolderSelectionViewModel"/> over the shared services on every call.</summary>
/// <param name="tree">Lists one level of the merged folder tree.</param>
/// <param name="rules">Reads the stored selection rules.</param>
/// <param name="writer">Saves the staged changes.</param>
/// <param name="evaluator">Judges folders against the rules.</param>
/// <param name="pairReader">Tells which pairs have synced.</param>
/// <param name="accounts">Names the shown account.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class FolderSelectionFactory(IFolderTreeSource tree, ISelectionRuleReader rules, ILeasedSelectionRuleWriter writer, ISyncSelectionEvaluator evaluator, ISyncPairReader pairReader, IAccountStore accounts, ILocalizationService localization) : IFolderSelectionFactory
{
    /// <inheritdoc />
    public FolderSelectionViewModel CreatePage() => new(tree, rules, writer, evaluator, pairReader, accounts, localization) { Hosting = FolderSelectionHostingFactory.CreatePage() };
}
