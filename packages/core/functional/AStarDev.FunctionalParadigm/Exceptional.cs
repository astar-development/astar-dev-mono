namespace AStarDev.FunctionalParadigm;

/// <summary>Factory methods for constructing <see cref="Exceptional{T}" /> instances.</summary>
public static class Exceptional
{
    /// <summary>Creates a <see cref="Success{T}" /> instance from a value of type <typeparamref name="T" />.</summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>An <see cref="Exceptional{T}" /> in the <see cref="Success{T}" /> state.</returns>
    public static Exceptional<T> Success<T>(T value) => new Success<T>(value);

    /// <summary>Creates a <see cref="Failure{T}" /> instance from a captured <see cref="Exception" />.</summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="exception">The captured exception.</param>
    /// <returns>An <see cref="Exceptional{T}" /> in the <see cref="Failure{T}" /> state.</returns>
    public static Exceptional<T> Failure<T>(Exception exception) => new Failure<T>(exception);
}
