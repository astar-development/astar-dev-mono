using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace AStarDev.EFCoreSqlite;

/// <summary>
/// Generates a new value-carrying strong id whenever the current value equals <c>default(TId)</c>,
/// so an explicitly supplied id is preserved and only an empty/default id is replaced on insert.
/// </summary>
/// <typeparam name="TId">The strong id type, expected to wrap a single <see cref="Guid" /> value.</typeparam>
/// <param name="create">Builds a <typeparamref name="TId" /> from a newly generated <see cref="Guid" />.</param>
public sealed class StrongGuidIdValueGenerator<TId>(Func<Guid, TId> create) : ValueGenerator<TId>
{
    /// <inheritdoc/>
    public override TId Next(EntityEntry entry) => create(Guid.CreateVersion7());

    /// <inheritdoc/>
    public override bool GeneratesTemporaryValues => false;
}
