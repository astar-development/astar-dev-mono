namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Factory methods for creating instances of <see cref="ValidationError" />.
/// </summary>
public static class ValidationErrorFactory
{
    /// <summary>
    ///     Creates a <see cref="ValidationError" /> for the specified property and message.
    /// </summary>
    public static ValidationError Create(string property, string message) => new(property, message);
}
