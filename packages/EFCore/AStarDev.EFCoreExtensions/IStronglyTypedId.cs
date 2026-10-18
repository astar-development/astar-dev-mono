namespace AStarDev.EFCoreExtensions;

/// <summary>
/// Marks a type as a strongly typed ID that wraps a single underlying value, e.g. an <see cref="int"/> or <see cref="Guid"/>.
/// Prefer one of the thin per-type interfaces (such as <see cref="IStronglyTypedIntId{TSelf}"/>) so the EF Core
/// configuration extensions can infer their type arguments.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type (curiously recurring), needed for the static abstract members.</typeparam>
/// <typeparam name="TValue">The underlying value type stored in the database.</typeparam>
public interface IStronglyTypedId<TSelf, TValue>
    where TSelf : IStronglyTypedId<TSelf, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value persisted to the database.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Gets the ID that represents "no ID yet". EF Core treats it as the sentinel meaning the value should be generated on add.
    /// </summary>
    static abstract TSelf Empty { get; }

    /// <summary>
    /// Creates an ID from its underlying value, e.g. when materialising an entity from the database.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    /// <returns>A new ID wrapping <paramref name="value"/>.</returns>
    static abstract TSelf Create(TValue value);
}
