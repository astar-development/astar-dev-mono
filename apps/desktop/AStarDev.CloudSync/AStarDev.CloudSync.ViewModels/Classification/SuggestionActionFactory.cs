namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="SuggestionAction"/> cases.</summary>
public static class SuggestionActionFactory
{
    /// <summary>Creates the <see cref="SuggestionAction.Promote"/> case.</summary>
    public static SuggestionAction CreatePromote() => new SuggestionAction.Promote();

    /// <summary>Creates the <see cref="SuggestionAction.Ignore"/> case.</summary>
    public static SuggestionAction CreateIgnore() => new SuggestionAction.Ignore();
}
