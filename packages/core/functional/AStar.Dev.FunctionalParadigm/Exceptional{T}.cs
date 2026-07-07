namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents the outcome of an operation that may either succeed with a
///     <typeparamref name="T" /> value or fail with a captured <see cref="Exception" />.
///     Use the <see cref="Exceptional" /> factory class to construct instances.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public abstract record Exceptional<T>
{
    /// <summary>
    ///     Restricts derivation of <see cref="Exceptional{T}" /> to <see cref="Success{T}" /> and
    ///     <see cref="Failure{T}" />, both declared in this assembly.
    /// </summary>
    private protected Exceptional()
    {
    }

    /// <summary>
    ///     Implicitly lifts a success value into an <see cref="Exceptional{T}" />.
    /// </summary>
    public static implicit operator Exceptional<T>(T value) => new Success<T>(value);

    /// <summary>
    ///     Implicitly lifts a captured exception into an <see cref="Exceptional{T}" />.
    /// </summary>
    public static implicit operator Exceptional<T>(Exception exception) => new Failure<T>(exception);
}

/// <summary>
///     Represents a successful <see cref="Exceptional{T}" /> carrying a value.
/// </summary>
public sealed record Success<T>(T Value) : Exceptional<T>;

/// <summary>
///     Represents a failed <see cref="Exceptional{T}" /> carrying the captured exception.
/// </summary>
public sealed record Failure<T>(Exception Exception) : Exceptional<T>;
