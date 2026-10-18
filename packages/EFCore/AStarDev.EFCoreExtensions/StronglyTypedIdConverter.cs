using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStarDev.EFCoreExtensions;

/// <summary>
/// EF Core value converter that stores a strongly typed ID as its underlying value.
/// </summary>
/// <typeparam name="TId">The strongly typed ID type.</typeparam>
/// <typeparam name="TValue">The underlying value type stored in the database.</typeparam>
public sealed class StronglyTypedIdConverter<TId, TValue> : ValueConverter<TId, TValue>
    where TId : IStronglyTypedId<TId, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StronglyTypedIdConverter{TId, TValue}"/> class.
    /// </summary>
    public StronglyTypedIdConverter()
        : base(id => id.Value, value => Create(value))
    { }

    // Expression trees cannot call static abstract members directly, so wrap TId.Create.
    private static TId Create(TValue value) => TId.Create(value);
}
