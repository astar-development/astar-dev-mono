namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents the outcome of a validation that may either succeed with a
///     <typeparamref name="T" /> value or fail with one or more <see cref="ValidationError" />s.
///     Use the <see cref="Validation" /> factory class to construct instances.
/// </summary>
/// <typeparam name="T">The type of the validated value.</typeparam>
public abstract record Validation<T>;

/// <summary>
///     Represents a successful <see cref="Validation{T}" /> carrying the validated value.
/// </summary>
public sealed record Valid<T>(T Value) : Validation<T>;

/// <summary>
///     Represents a failed <see cref="Validation{T}" /> carrying the accumulated validation errors.
/// </summary>
public sealed record Invalid<T>(IReadOnlyList<ValidationError> Errors) : Validation<T>;
