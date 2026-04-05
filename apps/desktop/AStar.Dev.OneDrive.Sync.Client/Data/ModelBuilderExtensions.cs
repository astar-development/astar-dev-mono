
using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

public static class ModelBuilderExtensions
{
    public static void UseSqliteFriendlyConversions(this ModelBuilder mb)
    {
        Type[] targetEntities =
        [
            typeof(AccountEntity),
    typeof(SyncFolderEntity),
    typeof(SyncConflictEntity),
    typeof(SyncJobEntity),
        ];

        foreach(var et in mb.Model.GetEntityTypes().Where(e => targetEntities.Contains(e.ClrType)))
        {
            ApplyConversionsForEntity(mb, et);
        }
    }

    private static void ApplyConversionsForEntity(ModelBuilder mb, IMutableEntityType et)
    {
        var eb = mb.Entity(et.ClrType);

        foreach(var propInfo in et.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = propInfo.PropertyType;

            if(propertyType == typeof(DateTimeOffset))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            }
            else if(Nullable.GetUnderlyingType(propertyType) == typeof(DateTimeOffset))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            }
            else if(propertyType == typeof(TimeSpan))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType("INTEGER");
            }
            else if(Nullable.GetUnderlyingType(propertyType) == typeof(TimeSpan))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType("INTEGER");
            }
            else if(propertyType == typeof(Guid))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType("BLOB");
            }
            else if(Nullable.GetUnderlyingType(propertyType) == typeof(Guid))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType("BLOB");
            }
            else if(propertyType == typeof(decimal))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType("INTEGER");
            }
            else if(Nullable.GetUnderlyingType(propertyType) == typeof(decimal))
            {
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType("INTEGER");
            }
            else if(propertyType.IsEnum)
            {
                _ = eb.Property(propInfo.Name).HasConversion<int>().HasColumnType("INTEGER");
            }
            else if(Nullable.GetUnderlyingType(propertyType)?.IsEnum == true)
            {
                var enumType = Nullable.GetUnderlyingType(propertyType);
                if(enumType != null)
                {
                    var converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(enumType, typeof(int));
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    _ = eb.Property(propInfo.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
            }
        }
    }
}
