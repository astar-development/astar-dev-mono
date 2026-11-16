namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>What the user is choosing a category for.</summary>
public abstract record SuggestionAction
{
    private SuggestionAction()
    {
    }

    /// <summary>Make the word a term in the chosen category.</summary>
    public sealed record Promote : SuggestionAction;

    /// <summary>Ignore the word. The chosen category is only used if the term has to be created.</summary>
    public sealed record Ignore : SuggestionAction;
}
