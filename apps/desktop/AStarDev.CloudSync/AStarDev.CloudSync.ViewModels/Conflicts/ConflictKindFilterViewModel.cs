using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One choice of the conflict kind filter: every kind, or one.</summary>
public sealed class ConflictKindFilterViewModel : LocalizedObservableObject
{
    internal ConflictKindFilterViewModel(Option<ConflictKind> kind, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{kind}");

    /// <summary>The kind this choice shows, or none for every kind.</summary>
    public Option<ConflictKind> Kind => throw new NotImplementedException(nameof(Kind));

    /// <summary>The choice's label.</summary>
    public string Text => throw new NotImplementedException(nameof(Text));
}
