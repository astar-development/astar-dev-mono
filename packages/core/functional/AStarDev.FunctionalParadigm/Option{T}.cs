namespace AStarDev.FunctionalParadigm;

/// <summary>Represents an optional value that may be present (<see cref="Some" />) or absent (<see cref="None" />).</summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
public abstract record Option<T>
{
    private Option()
    {
    }

    /// <summary>Implicitly converts a value to an <see cref="Option{T}" />.</summary>
    /// <param name="value">The value to wrap. Null becomes <see cref="None" />.</param>
    public static implicit operator Option<T>(T? value) =>
        value != null
            ? new Some(value)
            : None.Instance;

    /// <summary>Pattern matches on the option.</summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSome">Function to run when the value is present.</param>
    /// <param name="onNone">Function to run when the value is absent.</param>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        this switch
        {
            Some some => onSome(some.Value),
            None _ => onNone(),
            _ => throw new InvalidOperationException("It should not be possible to reach this point.")
        };

    /// <summary>Represents the presence of a value.</summary>
    public sealed record Some : Option<T>
    {
        /// <summary>Initializes a new instance of the <see cref="Option{T}.Some" /> class.</summary>
        /// <param name="value">A non-null value.</param>
        /// <exception cref="ArgumentNullException" />
        public Some(T value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        /// <summary>The wrapped value.</summary>
        public T Value { get; }

        /// <summary>Overrides the ToString method to return both the type and the value.</summary>
        /// <returns>The overridden ToString</returns>
        public override string ToString() => $"Some({Value})";
    }

    /// <summary>Represents the absence of a value.</summary>
    public sealed record None : Option<T>
    {
        /// <summary>A helper method to create an instance of <see cref="Option{T}.None" /></summary>
        public static readonly None Instance = new();

        private None()
        {
        }

        /// <summary>Overrides the ToString method to return the type as a simple string.</summary>
        /// <returns>The overridden ToString</returns>
        public override string ToString() => "None";
    }
}
