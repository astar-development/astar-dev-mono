namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents an optional value that may be present (<see cref="Some" />) or absent (<see cref="None" />).
/// </summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
public abstract class Option<T>
{
    private Option()
    {
    }

    /// <summary>
    ///     Implicitly converts a value to an <see cref="Option{T}" />.
    /// </summary>
    /// <param name="value">The value to wrap. Null becomes <see cref="None" />.</param>
    public static implicit operator Option<T>(T value) =>
        value != null
            ? new Some(value)
            : None.Instance;

    /// <summary>
    ///     Pattern matches on the option.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSome">Function to run when the value is present.</param>
    /// <param name="onNone">Function to run when the value is absent.</param>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        this switch
        {
            Some some => onSome(some.Value),
            None _    => onNone(),
            _         => throw new InvalidOperationException("It should not be possible to reach this point.")
        };

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="Option{T}" />.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Option<T> other && this switch
                                  {
                                      Some some => other is Some otherSome && EqualityComparer<T>.Default.Equals(some.Value, otherSome.Value),
                                      None      => other is None,
                                      _         => false
                                  };

    /// <summary>
    ///     Returns a hash code for the current instance of the <see cref="Option{T}" />.
    ///     For <see cref="Option{T}.Some" />, the hash code is computed based on its type and value.
    ///     For <see cref="Option{T}.None" />, the hash code is derived from its type.
    /// </summary>
    /// <returns>
    ///     An integer representing the hash code of the current instance.
    /// </returns>
    public override int GetHashCode() =>
        this switch
        {
            Some some => HashCode.Combine(typeof(Some), some.Value),
            None      => typeof(None).GetHashCode(),
            _         => 0
        };

    /// <summary>
    ///     Determines whether two <see cref="Option{T}" /> instances are equal at a value level.
    /// </summary>
    /// <param name="left">The first <see cref="Option{T}" /> instance to compare.</param>
    /// <param name="right">The second <see cref="Option{T}" /> instance to compare.</param>
    /// <returns>
    ///     True if the two <see cref="Option{T}" /> instances are considered equal; otherwise, false.
    /// </returns>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="Option{T}" /> instances are not equal at a value level.
    /// </summary>
    /// <param name="left">The first <see cref="Option{T}" /> instance to compare.</param>
    /// <param name="right">The second <see cref="Option{T}" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the two instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    /// <summary>
    ///     Represents the presence of a value.
    /// </summary>
    public sealed class Some : Option<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Option{T}.Some" /> class.
        /// </summary>
        /// <param name="value">A non-null value.</param>
        /// <exception cref="ArgumentNullException" />
        public Some(T value)
        {
            if(value is null) throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        /// <summary>
        ///     The wrapped value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///     Overrides the ToString method to return both the type and the value.
        /// </summary>
        /// <returns>The overridden ToString</returns>
        public override string ToString() => $"Some({Value})";
    }

    /// <summary>
    ///     Represents the absence of a value.
    /// </summary>
    public sealed class None : Option<T>
    {
        /// <summary>
        ///     A helper method to create an instance of <see cref="Option{T}.None" />
        /// </summary>
        public static readonly None Instance = new();

        private None()
        {
        }

        /// <summary>
        ///     Overrides the ToString method to return the type as a simple string.
        /// </summary>
        /// <returns>The overridden ToString</returns>
        public override string ToString() => "None";
    }
}
