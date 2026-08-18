
using System.Reflection;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

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
        Type[] targetEntities =
        [
            typeof(AccountEntity),
    typeof(SyncConflictEntity),
    typeof(SyncJobEntity),
    typeof(FileAccessDetailEntity),
    typeof(DeletionStatusEntity),
    typeof(EventEntity),
    typeof(TagToIgnoreEntity),
    typeof(ModelToIgnoreEntity),
    typeof(FileClassificationCategoryEntity),
    typeof(ScrapeConfigurationEntity),
    typeof(ConnectionStringsEntity),
    typeof(UserConfigurationEntity),
    typeof(SearchConfigurationEntity),
    typeof(SearchCategoryEntity),
    typeof(ScrapeDirectoriesEntity),
        ];

        foreach (var et in mb.Model.GetEntityTypes().Where(e => targetEntities.Contains(e.ClrType)))
        {
            ApplyConversionsForEntity(mb, et);
        }
    }

    private static void ApplyConversionsForEntity(ModelBuilder mb, IMutableEntityType et)
    {
        var eb = mb.Entity(et.ClrType);

        foreach (var propInfo in et.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = propInfo.PropertyType;

            if (propertyType == typeof(DateTimeOffset))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks).HasColumnType(IntegerColumnType).HasColumnName(propInfo.Name + Ticks);
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(DateTimeOffset))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks).HasColumnType(IntegerColumnType).HasColumnName(propInfo.Name + Ticks);
            }
            else if (propertyType == typeof(TimeSpan))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType(IntegerColumnType);
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(TimeSpan))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType(IntegerColumnType);
            }
            else if (propertyType == typeof(Guid))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType(BlobColumnType);
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(Guid))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType(BlobColumnType);
            }
            else if (propertyType == typeof(decimal))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType(IntegerColumnType);
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(decimal))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType(IntegerColumnType);
            }
            else if (propertyType.IsEnum)
            {
                _ = eb.Property(propInfo.Name).HasConversion<int>().HasColumnType(IntegerColumnType);
            }
            else if (Nullable.GetUnderlyingType(propertyType)?.IsEnum == true)
            {
                var enumType = Nullable.GetUnderlyingType(propertyType);
                if (enumType != null)
                {
                    var converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(enumType, typeof(int));
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    _ = eb.Property(propInfo.Name).HasConversion(converter).HasColumnType(IntegerColumnType);
                }
            }
        }
    }
}
