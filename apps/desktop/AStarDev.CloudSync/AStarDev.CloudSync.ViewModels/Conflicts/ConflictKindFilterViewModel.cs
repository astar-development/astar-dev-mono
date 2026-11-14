using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One choice of the conflict kind filter: every kind, or one.</summary>
public sealed class ConflictKindFilterViewModel : LocalizedObservableObject
{
    internal ConflictKindFilterViewModel(Option<ConflictKind> kind, ILocalizationService localization) : base(localization) => Kind = kind;

    /// <summary>The kind this choice shows, or none for every kind.</summary>
    public Option<ConflictKind> Kind { get; }

    /// <summary>The choice's label.</summary>
    public string Text => Localization.GetLocal(Kind.Match(ConflictTexts.KindKey, () => "Conflict.Filter.All"));
}
