namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///    Represents a type with a single value, used to indicate the absence of a meaningful value.
/// </summary>
public record Unit
{
    /// <summary>
    ///   The single instance of <see cref="Unit" />.
    /// </summary>
    public static readonly Unit Instance = new();
}
