namespace AStarDev.CloudSync.Classification;

/// <summary>What the suggestions list can tell the user about a suggested word.</summary>
public abstract record SuggestionHint
{
    private SuggestionHint()
    {
    }

    /// <summary>Nothing special is known about the word.</summary>
    public sealed record Plain : SuggestionHint
    {
        internal Plain()
        {
        }
    }

    /// <summary>The word is a known first name, so the UI can hint Person. It is never assigned automatically.</summary>
    public sealed record FirstName : SuggestionHint
    {
        internal FirstName()
        {
        }
    }
}
