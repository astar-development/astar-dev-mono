namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SuggestionHint"/> values.</summary>
public static class SuggestionHintFactory
{
    /// <summary>Creates the plain hint.</summary>
    public static SuggestionHint CreatePlain() => new SuggestionHint.Plain();

    /// <summary>Creates the first-name hint.</summary>
    public static SuggestionHint CreateFirstName() => new SuggestionHint.FirstName();
}
