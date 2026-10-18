using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.EFCoreExtensions;

/// <summary>
/// <see cref="EntityTypeBuilder{TEntity}"/> extensions that map strongly typed IDs as entity keys.
/// </summary>
public static class StronglyTypedIdExtensions
{
    /// <summary>
    /// Maps a strongly typed int ID as the key and lets the database generate the value when the ID is <c>TId.Empty</c>.
    /// Defaults to SQLite. Other providers use their default identity strategy for generated integer keys.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The strongly typed int ID type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="idProperty">Selects the ID property on the entity.</param>
    /// <param name="provider">The database provider, which decides how the value is generated. Defaults to <see cref="DatabaseProvider.Sqlite"/>.</param>
    /// <returns>The property builder, for further configuration.</returns>
    public static PropertyBuilder<TId> HasGeneratedIntId<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TId>> idProperty,
        DatabaseProvider provider = DatabaseProvider.Sqlite)
        where TEntity : class
        where TId : IStronglyTypedIntId<TId>
        => builder.HasDatabaseGeneratedKey<TEntity, TId, int>(idProperty, provider);

    /// <summary>
    /// Maps a strongly typed long ID as the key and lets the database generate the value when the ID is <c>TId.Empty</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The strongly typed long ID type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="idProperty">Selects the ID property on the entity.</param>
    /// <param name="provider">The database provider, which decides how the value is generated. Defaults to <see cref="DatabaseProvider.Sqlite"/>.</param>
    /// <returns>The property builder, for further configuration.</returns>
    public static PropertyBuilder<TId> HasGeneratedLongId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, TId>> idProperty, DatabaseProvider provider = DatabaseProvider.Sqlite)
        where TEntity : class
        where TId : IStronglyTypedLongId<TId>
        => builder.HasDatabaseGeneratedKey<TEntity, TId, long>(idProperty, provider);

    /// <summary>
    /// Maps a strongly typed Guid ID as the key. EF generates the value when the ID is <c>TId.Empty</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The strongly typed Guid ID type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="idProperty">Selects the ID property on the entity.</param>
    /// <returns>The property builder, for further configuration.</returns>
    public static PropertyBuilder<TId> HasGeneratedGuidId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, TId>> idProperty)
        where TEntity : class
        where TId : IStronglyTypedGuidId<TId>
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HasStronglyTypedKey<TEntity, TId, Guid>(idProperty)
            .ValueGeneratedOnAdd()
            .HasSentinel(TId.Empty);
    }

    /// <summary>
    /// Maps a strongly typed string ID as the key. The application supplies the value.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The strongly typed string ID type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="idProperty">Selects the ID property on the entity.</param>
    /// <returns>The property builder, for further configuration.</returns>
    public static PropertyBuilder<TId> HasStringId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, TId>> idProperty)
        where TEntity : class
        where TId : IStronglyTypedStringId<TId>
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HasStronglyTypedKey<TEntity, TId, string>(idProperty)
            .ValueGeneratedNever();
    }

    private static PropertyBuilder<TId> HasDatabaseGeneratedKey<TEntity, TId, TValue>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, TId>> idProperty, DatabaseProvider provider)
        where TEntity : class
        where TId : IStronglyTypedId<TId, TValue>
        where TValue : notnull
    {
        var property = builder.HasStronglyTypedKey<TEntity, TId, TValue>(idProperty)
            .ValueGeneratedOnAdd()
            .HasSentinel(TId.Empty);

        if (provider == DatabaseProvider.Sqlite)
        {
            property.UseAutoincrement();
        }

        return property;
    }

    private static PropertyBuilder<TId> HasStronglyTypedKey<TEntity, TId, TValue>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, TId>> idProperty)
        where TEntity : class
        where TId : IStronglyTypedId<TId, TValue>
        where TValue : notnull
    {
        var property = builder.Property(idProperty)
            .HasConversion<StronglyTypedIdConverter<TId, TValue>>();

        builder.HasKey(property.Metadata.Name);

        return property;
    }
}
