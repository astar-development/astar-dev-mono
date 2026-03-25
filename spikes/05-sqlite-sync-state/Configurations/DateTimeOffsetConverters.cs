using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Spikes.SqliteSyncState.Configurations;

/// <summary>
/// Shared EF Core value converters for DateTimeOffset columns.
/// SQLite has no native DateTimeOffset type; storing as Unix milliseconds (long)
/// enables server-side ORDER BY and WHERE comparisons. See decision DB-01.
/// </summary>
public static class DateTimeOffsetConverters
{
    public static readonly ValueConverter<DateTimeOffset, long> ToUnixMs =
        new(v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

    public static readonly ValueConverter<DateTimeOffset?, long?> ToUnixMsNullable =
        new(v => v == null ? null : v.Value.ToUnixTimeMilliseconds(),
            v => v == null ? null : DateTimeOffset.FromUnixTimeMilliseconds(v.Value));
}
