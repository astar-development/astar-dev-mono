namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents a single validation failure against a named property.
///     Use <see cref="ValidationErrorFactory" /> to construct instances.
/// </summary>
/// <param name="Property">The name of the property that failed validation.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ValidationError(string Property, string Message);
