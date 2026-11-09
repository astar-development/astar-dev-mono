namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the selection rules (#1397). The folder tree and the sync engine both ask it, so the tree can never show a folder as included that the engine skips.</summary>
public interface ISyncSelectionEvaluator
{
    /// <summary>Judges a folder. The most specific covering rule wins; an Exclude beats an Include on the same path; rules match whole folder names only, ignoring case. A folder with no covering rule is <see cref="SelectionState.Uncovered"/>. When the folder's own state differs from a stored rule below it, the result is <see cref="SelectionState.PartiallyIncluded"/>; this looks at the stored rules only, never at loaded tree nodes. A nested Include reports the pair and direction of its top-most enclosing Include.</summary>
    /// <param name="path">The folder, relative to the sync root.</param>
    /// <param name="rules">The account's stored rules.</param>
    SelectionState Evaluate(LocalRelativePath path, IReadOnlyList<SelectionRule> rules);

    /// <summary>Whether the folder syncs: a covering Include rule decides, ignoring any rules below it. This is what the sync engine asks.</summary>
    /// <param name="path">The folder, relative to the sync root.</param>
    /// <param name="rules">The account's stored rules.</param>
    bool IsIncluded(LocalRelativePath path, IReadOnlyList<SelectionRule> rules);
}
