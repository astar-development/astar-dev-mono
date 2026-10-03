
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStarDev.EFCoreSqlite;

/// <summary>Extension methods that register SQLite-friendly value conversions on a <see cref="ModelBuilder"/>.</summary>
public static class ModelBuilderExtensions
{
    private const string IntegerColumnType = "INTEGER";
    private const string BlobColumnType = "BLOB";
    private const string Ticks = "_Ticks";

    /// <summary>Applies the SQLite-friendly value conversions to every configured entity type that needs them.</summary>
    /// <param name="mb">The model builder to configure.</param>
    public static void UseSqliteFriendlyConversions(this ModelBuilder mb)
    {
        ArgumentNullException.ThrowIfNull(mb);

        foreach (var et in mb.Model.GetEntityTypes())
        {
            ConfigureEntity(mb, et);
        }
    }

    private static void ConfigureEntity(ModelBuilder mb, IMutableEntityType entityType)
    {
        var entityBuilder = mb.Entity(entityType.ClrType);

        foreach (var property in entityType.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (entityType.FindNavigation(property.Name) is not null
                || entityType.FindSkipNavigation(property.Name) is not null
                || !RequiresSqliteConversion(property.PropertyType))
            {
                continue;
            }

            if (entityType.FindProperty(property.Name)?.GetValueConverter() is not null) continue;

            var propertyBuilder = entityBuilder.Property(property.Name);
            ConfigureProperty(propertyBuilder, property.PropertyType, property.Name);
        }
    }

    private static void ConfigureProperty(PropertyBuilder propertyBuilder, Type propertyType, string propertyName)
    {
        var nullableType = Nullable.GetUnderlyingType(propertyType);

        if (propertyType == typeof(DateTimeOffset))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks)
                .HasColumnType(IntegerColumnType)
                .HasColumnName(propertyName + Ticks);
        }
        else if (nullableType == typeof(DateTimeOffset))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks)
                .HasColumnType(IntegerColumnType)
                .HasColumnName(propertyName + Ticks);
        }
        else if (propertyType == typeof(TimeSpan))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType(IntegerColumnType);
        }
        else if (nullableType == typeof(TimeSpan))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType(IntegerColumnType);
        }
        else if (propertyType == typeof(Guid))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType(BlobColumnType);
        }
        else if (nullableType == typeof(Guid))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType(BlobColumnType);
        }
        else if (propertyType == typeof(decimal))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType(IntegerColumnType);
        }
        else if (nullableType == typeof(decimal))
        {
            _ = propertyBuilder.HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType(IntegerColumnType);
        }
        else if (propertyType.IsEnum)
        {
            _ = propertyBuilder.HasConversion<int>().HasColumnType(IntegerColumnType);
        }
        else if (nullableType?.IsEnum == true)
        {
            var converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(nullableType, typeof(int));
            var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
            _ = propertyBuilder.HasConversion(converter).HasColumnType(IntegerColumnType);
        }
    }

    private static bool RequiresSqliteConversion(Type propertyType)
    {
        var nullableType = Nullable.GetUnderlyingType(propertyType);
        var type = nullableType ?? propertyType;

        return type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type.IsEnum;
    }
}
