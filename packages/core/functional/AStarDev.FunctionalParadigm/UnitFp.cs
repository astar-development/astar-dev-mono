namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///    Represents a type with a single value, used to indicate the absence of a meaningful value.
/// </summary>
public record UnitFp
{
    /// <summary>
    ///   The single instance of <see cref="UnitFp" />.
    /// </summary>
    public static readonly UnitFp Instance = new();
}
