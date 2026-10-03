namespace AStarDev.FunctionalParadigm;

/// <summary>Factory methods for constructing <see cref="Option{T}" /> instances.</summary>
public static class Option
{
    /// <summary>Creates an <see cref="Option{T}" /> in the present state, wrapping <paramref name="value" />.</summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    /// <param name="value">A non-null value.</param>
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);

    /// <summary>Creates an <see cref="Option{T}" /> in the absent state.</summary>
    /// <typeparam name="T">The type the option would have wrapped.</typeparam>
    public static Option<T> None<T>() => Option<T>.None.Instance;
}
