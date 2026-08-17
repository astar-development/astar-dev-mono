namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents a single validation failure against a named property.
///     Use <see cref="ValidationErrorFactory" /> to construct instances.
/// </summary>
/// <param name="Property">The name of the property that failed validation.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ValidationError(string Property, string Message);

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
