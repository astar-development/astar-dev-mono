using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ImmutableDomain.EntityFrameworkCore.Implementation;

[ExcludeFromCodeCoverage]
public static class ImmutableUpdateExtensions
{
    public static void UpdateImmutable<T>(this DbContext dbContext, T modified) where T : class
    {
        var key = dbContext.GetStableKey(modified);
        var entry = dbContext.Set<T>().Local.FindEntry(key.EntityKey.Properties, key.Values) ?? throw new InvalidOperationException("Original entity must be tracked.");
        entry.State = EntityState.Detached;
        dbContext.Attach(modified);
        dbContext.Entry(modified).State = EntityState.Modified;

        // Align navigation graphs with the immutable copy
        foreach (var navigation in entry.Navigations)
        {
            if (navigation is CollectionEntry collectionEntry) dbContext.UpdateCollection(collectionEntry, modified);
            else if (navigation is ReferenceEntry referenceEntry) dbContext.UpdateReference(referenceEntry, modified);
        }
    }

    private static void UpdateCollection(this DbContext dbContext, CollectionEntry collectionEntry, object modifiedEntity)
    {
        var modifiedCollection = GetCollectionValue(modifiedEntity, collectionEntry.Metadata);

        if (collectionEntry.CurrentValue is not IList trackedCollection || modifiedCollection is null) return;

        var key = collectionEntry.Metadata.TargetEntityType.FindPrimaryKey() ?? throw new InvalidOperationException("Unable to determine key properties for collection items.");
        if (key.Properties.Any(key => key.IsShadowProperty())) throw new InvalidOperationException("Cannot update collections with shadow key properties.");

        var originalEntries = trackedCollection
            .OfType<object>()
            .Select(item => (item, key: item.TryGetStableKey(key)))
            .Where(pair => pair.key is not null)
            .ToDictionary(pair => pair.key!, pair => pair.item);

        var updateMethod = GetUpdateImmutableMethod(collectionEntry.Metadata.TargetEntityType.ClrType);

        foreach (object modifiedItem in modifiedCollection.OfType<object>())
        {
            var keyValue = modifiedItem.TryGetStableKey(key);
            if (keyValue is not null && originalEntries.ContainsKey(keyValue))
            {
                updateMethod.Invoke(null, [dbContext, modifiedItem]);
            }
            else
            {
                dbContext.Entry(modifiedItem).State = EntityState.Added;
            }
        }

        foreach (object? itemToRemove in originalEntries.Values)
        {
            dbContext.Remove(itemToRemove);
        }
    }

    private static MethodInfo GetUpdateImmutableMethod(Type entityType) =>
        typeof(ImmutableUpdateExtensions)
            .GetMethod(nameof(UpdateImmutable), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(entityType)
            ?? throw new InvalidOperationException("Unable to get UpdateImmutable method.");

    private static CompositeKey GetStableKey(this DbContext dbContext, object entity) =>
        dbContext.TryGetStableKey(entity) is CompositeKey key ? key
        : throw new InvalidOperationException("Entity must have a valid stable key.");

    private static CompositeKey? TryGetStableKey(this DbContext dbContext, object entity)
    {
        var key = (dbContext.Model.FindEntityType(entity.GetType())?.FindPrimaryKey()) ?? throw new InvalidOperationException("Unable to determine primary key for entity.");

        return entity.TryGetStableKey(key);
    }

    private static CompositeKey? TryGetStableKey(this object entity, IKey key)
    {
        var keyProperties = key.Properties;
        object[] values = new object[keyProperties.Count];

        for (int i = 0; i < keyProperties.Count; i++)
        {
            var prop = keyProperties[i];
            if (prop.IsShadowProperty()) throw new InvalidOperationException($"Cannot get key value for shadow property {prop.Name}.");

            var propertyInfo = prop.PropertyInfo
                ?? entity.GetType().GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Key property '{prop.Name}' not found on type '{entity.GetType().Name}'.");

            object? value = propertyInfo.GetValue(entity);

            object? defaultValue = prop.ClrType.IsValueType ? Activator.CreateInstance(prop.ClrType) : null;
            if (Equals(value, defaultValue)) return null;   // key has default value

            values[i] = value!;                               // not a default, safe to use
        }

        return new CompositeKey(key, [.. values]);
    }

#pragma warning disable CA1852
    private record CompositeKey(IKey EntityKey, object[] Values) : IEquatable<CompositeKey>
    {
        public virtual bool Equals(CompositeKey? other) =>
            other is not null && Values.SequenceEqual(other.Values);

        public override int GetHashCode()
        {
            var hc = new HashCode();
            foreach (object v in Values) hc.Add(v);
            return hc.ToHashCode();
        }
    }
#pragma warning restore CA1852

    private static void UpdateReference(this DbContext dbContext, ReferenceEntry referenceEntry, object modifiedEntity)
    {
        object? modifiedReference = GetNavigationValue(modifiedEntity, referenceEntry.Metadata);
        if (modifiedReference is null) return;

        if (referenceEntry.TargetEntry is { Entity: { } })
        {
            var updateMethod = GetUpdateImmutableMethod(referenceEntry.Metadata.TargetEntityType.ClrType);
            updateMethod.Invoke(null, [dbContext, modifiedReference]);
            return;
        }

        referenceEntry.CurrentValue = modifiedReference;   // not tracked - start tracking it

        var key = referenceEntry.Metadata.TargetEntityType.FindPrimaryKey()
            ?? throw new InvalidOperationException("Unable to determine entity key for reference target.");

        var stableKey = modifiedReference.TryGetStableKey(key);
        dbContext.Entry(modifiedReference).State = stableKey is null ? EntityState.Added : EntityState.Unchanged;
    }

    private static IEnumerable? GetCollectionValue(this object source, INavigationBase navigation) =>
        GetNavigationValue(source, navigation) as IEnumerable;

    private static object? GetNavigationValue(this object source, INavigationBase navigation) =>
        navigation.PropertyInfo is { } propertyInfo ? propertyInfo.GetValue(source)
        : navigation.FieldInfo is { } fieldInfo ? fieldInfo.GetValue(source)
        : null;
}
