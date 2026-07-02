using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
///   The <see cref="SqliteTypeConverters"></see> class provides a set of value converters for handling specific data types in SQLite databases, ensuring compatibility and proper storage of these types. These converters facilitate the conversion of complex data types to simpler representations that can be stored in SQLite, and vice versa, allowing for seamless integration with Entity Framework Core.
/// </summary>
public static class SqliteTypeConverters
{
    /// <summary>
    ///  Gets a value converter that converts <see cref="DateTimeOffset" /> values to their corresponding UTC ticks representation for storage in SQLite, and vice versa. This converter ensures that <see cref="DateTimeOffset" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to <see cref="DateTimeOffset" /> when reading from the database.
    /// </summary>
    public static ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicks { get; } =
        new(dto => dto.ToUniversalTime().UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

/// <summary>
///   Gets a value converter that converts nullable <see cref="DateTimeOffset" /> values to their corresponding UTC ticks representation for storage in SQLite, and vice versa. This converter ensures that nullable <see cref="DateTimeOffset" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to nullable <see cref="DateTimeOffset" /> when reading from the database.
/// </summary>
    public static ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToTicks { get; } =
        new(dto => dto.HasValue ? dto.Value.ToUniversalTime().UtcTicks : null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

/// <summary>
///  Gets a value converter that converts <see cref="TimeSpan" /> values to their corresponding ticks representation for storage in SQLite, and vice versa. This converter ensures that <see cref="TimeSpan" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to <see cref="TimeSpan" /> when reading from the database.
/// </summary>
    public static ValueConverter<TimeSpan, long> TimeSpanToTicks { get; } =
        new(ts => ts.Ticks, ticks => TimeSpan.FromTicks(ticks));

/// <summary>
///  Gets a value converter that converts nullable <see cref="TimeSpan" /> values to their corresponding ticks representation for storage in SQLite, and vice versa. This converter ensures that nullable <see cref="TimeSpan" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to nullable <see cref="TimeSpan" /> when reading from the database.
/// </summary>
    public static ValueConverter<TimeSpan?, long?> NullableTimeSpanToTicks { get; } =
        new(ts => ts.HasValue ? ts.Value.Ticks : null, ticks => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : null);

/// <summary>
/// Gets a value converter that converts <see cref="Guid" /> values to their corresponding byte array representation for storage in SQLite, and vice versa. This converter ensures that <see cref="Guid" /> values are stored in a format compatible with SQLite's BLOB type, allowing for accurate retrieval and conversion back to <see cref="Guid" /> when reading from the database.
/// </summary>
    public static ValueConverter<Guid, byte[]> GuidToBytes { get; } =
        new(g => g.ToByteArray(), b => new Guid(b));

/// <summary>
/// Gets a value converter that converts nullable <see cref="Guid" /> values to their corresponding byte array representation for storage in SQLite, and vice versa. This converter ensures that nullable <see cref="Guid" /> values are stored in a format compatible with SQLite's BLOB type, allowing for accurate retrieval and conversion back to nullable <see cref="Guid" /> when reading from the database.
/// </summary>
    public static ValueConverter<Guid?, byte[]?> NullableGuidToBytes { get; } =
        new(g => g.HasValue ? g.Value.ToByteArray() : null, b => b != null ? new Guid(b) : null);

/// <summary>
///  Gets a value converter that converts <see cref="decimal" /> values to their corresponding long representation in cents for storage in SQLite, and vice versa. This converter ensures that <see cref="decimal" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to <see cref="decimal" /> when reading from the database.
/// </summary>
    public static ValueConverter<decimal, long> DecimalToCents { get; } =
        new(d => (long)Math.Round(d * 100m), l => l / 100m);

/// <summary>
/// Gets a value converter that converts nullable <see cref="decimal" /> values to their corresponding long representation in cents for storage in SQLite, and vice versa. This converter ensures that nullable <see cref="decimal" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to nullable <see cref="decimal" /> when reading from the database.
/// </summary>
    public static ValueConverter<decimal?, long?> NullableDecimalToCents { get; } =
        new(d => d.HasValue ? (long?)Math.Round(d.Value * 100m) : null, l => l.HasValue ? l.Value / 100m : null);

/// <summary>
///   Gets a value converter that converts <see cref="Option{T}" /> values to their corresponding nullable representation for storage in SQLite, and vice versa. This converter ensures that <see cref="Option{T}" /> values are stored in a format compatible with SQLite's nullable types, allowing for accurate retrieval and conversion back to <see cref="Option{T}" /> when reading from the database.
/// </summary>
    public static ValueConverter<Option<string>, string?> OptionStringToNullableString { get; } =
        new(opt => opt.Match<string?>(v => v, () => null),
            str => str != null ? Option.Some(str) : Option.None<string>());

/// <summary>
///  Gets a value converter that converts <see cref="Option{DateTimeOffset}" /> values to their corresponding nullable ticks representation for storage in SQLite, and vice versa. This converter ensures that <see cref="Option{DateTimeOffset}" /> values are stored in a format compatible with SQLite's INTEGER type, allowing for accurate retrieval and conversion back to <see cref="Option{DateTimeOffset}" /> when reading from the database.
/// </summary>
    public static ValueConverter<Option<DateTimeOffset>, long?> OptionDateTimeOffsetToNullableTicks { get; } =
        new(opt => opt.Match<long?>(v => v.ToUniversalTime().UtcTicks, () => null),
            ticks => ticks.HasValue ? Option.Some(new DateTimeOffset(ticks.Value, TimeSpan.Zero)) : Option.None<DateTimeOffset>());
}
