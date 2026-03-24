namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Represents a sealed class for defining distinct event types such as Add, Update, and Delete.
///     This class ensures that only predefined instances of event types can be used,
///     providing type safety and preventing the creation of arbitrary states.
/// </summary>
public sealed class EventType : IEquatable<EventType>
{
    /// <summary>
    ///     Private constructor to prevent external instantiation.
    ///     This ensures that only the static readonly instances above can be created.
    /// </summary>
    /// <param name="value">The integer value representing the event type.</param>
    /// <param name="name">The string name of the event type.</param>
    private EventType(int value, string name)
    {
        Value = value;
        Name  = name;
    }

    /// <summary>
    ///     Represents an 'Add' event type, typically used for new record creation.
    /// </summary>
    public static EventType Add => new(1, "Add");

    /// <summary>
    ///     Represents an 'Update' event type, typically used for modifying existing records.
    /// </summary>
    public static EventType Update => new(2, "Update");

    /// <summary>
    ///     Represents a 'SoftDelete' event type, typically used for 'soft' removing records.
    /// </summary>
    public static EventType SoftDelete => new(3, "SoftDelete");

    /// <summary>
    ///     Represents a 'HardDelete' event type, typically used for permanently removing records.
    /// </summary>
    public static EventType HardDelete => new(4, "HardDelete");

    /// <summary>
    ///     Gets the integer value associated with the event type.
    /// </summary>
    public int Value { get; }

    /// <summary>
    ///     Gets the string name of the event type.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public bool Equals(EventType? other)
    {
        if(other is null)
        {
            return false;
        }

#pragma warning disable IDE0046
        if(ReferenceEquals(this, other))
#pragma warning restore IDE0046
        {
            return true;
        }

        return Value == other.Value && Name == other.Name;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is EventType other && Equals(other));

    /// <summary>
    ///     Returns the string name of the event type, useful for debugging and display.
    /// </summary>
    /// <returns>The name of the event type.</returns>
    public override string ToString()
        => Name;

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Value, Name);

    /// <summary>
    ///     Overloads the equality operator to compare two <see cref="EventType" /> objects.
    /// </summary>
    /// <param name="left">The first <see cref="EventType" /> to compare.</param>
    /// <param name="right">The second <see cref="EventType" /> to compare.</param>
    /// <returns><c>true</c> if the two <see cref="EventType" /> objects are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(EventType left, EventType right)
#pragma warning disable IDE0041
        => left?.Equals(right) ?? ReferenceEquals(right, null);
#pragma warning restore IDE0041

    /// <summary>
    ///     Overloads the inequality operator to compare two <see cref="EventType" /> objects.
    /// </summary>
    /// <param name="left">The first <see cref="EventType" /> to compare.</param>
    /// <param name="right">The second <see cref="EventType" /> to compare.</param>
    /// <returns><c>true</c> if the two <see cref="EventType" /> objects are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(EventType left, EventType right)
        => !(left == right);
}