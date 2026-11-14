namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TransferConcurrency"/> values. A value outside the allowed range is clamped, never rejected.</summary>
public static class TransferConcurrencyFactory
{
    /// <summary>The fewest transfers at once.</summary>
    public const int Minimum = 1;

    /// <summary>The most transfers at once. The cloud throttles a single user well before more parallel transfers help.</summary>
    public const int Maximum = 8;

    /// <summary>What a new account starts with.</summary>
    public const int Default = 4;

    /// <summary>Creates a value, clamping <paramref name="requested"/> to the allowed range.</summary>
    /// <param name="requested">The number the user or a caller asked for.</param>
    public static TransferConcurrency Create(int requested) => new(Math.Clamp(requested, Minimum, Maximum));

    /// <summary>Creates the value a new account starts with.</summary>
    public static TransferConcurrency CreateDefault() => new(Default);
}
