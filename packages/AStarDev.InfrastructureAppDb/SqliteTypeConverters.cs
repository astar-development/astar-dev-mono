using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

/// <summary>Shared EF Core value converters for types SQLite cannot store natively.</summary>
public static class SqliteTypeConverters
{
    /// <summary>Converts a <see cref="DateTimeOffset"/> to and from UTC ticks.</summary>
    public static ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicks { get; } =
        new(dto => dto.ToUniversalTime().UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    /// <summary>Converts a nullable <see cref="DateTimeOffset"/> to and from nullable UTC ticks.</summary>
    public static ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToTicks { get; } =
        new(dto => dto.HasValue ? dto.Value.ToUniversalTime().UtcTicks : null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

    /// <summary>Converts a <see cref="TimeSpan"/> to and from ticks.</summary>
    public static ValueConverter<TimeSpan, long> TimeSpanToTicks { get; } =
        new(ts => ts.Ticks, ticks => TimeSpan.FromTicks(ticks));

    /// <summary>Converts a nullable <see cref="TimeSpan"/> to and from nullable ticks.</summary>
    public static ValueConverter<TimeSpan?, long?> NullableTimeSpanToTicks { get; } =
        new(ts => ts.HasValue ? ts.Value.Ticks : null, ticks => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : null);

    /// <summary>Converts a <see cref="Guid"/> to and from its byte array representation.</summary>
    public static ValueConverter<Guid, byte[]> GuidToBytes { get; } =
        new(g => g.ToByteArray(), b => new Guid(b));

    /// <summary>Converts a nullable <see cref="Guid"/> to and from its byte array representation.</summary>
    public static ValueConverter<Guid?, byte[]?> NullableGuidToBytes { get; } =
        new(g => g.HasValue ? g.Value.ToByteArray() : null, b => b != null ? new Guid(b) : null);

    /// <summary>Converts a <see cref="decimal"/> to and from an integral cents representation.</summary>
    public static ValueConverter<decimal, long> DecimalToCents { get; } =
        new(d => (long)Math.Round(d * 100m), l => l / 100m);

    /// <summary>Converts a nullable <see cref="decimal"/> to and from a nullable integral cents representation.</summary>
    public static ValueConverter<decimal?, long?> NullableDecimalToCents { get; } =
        new(d => d.HasValue ? (long?)Math.Round(d.Value * 100m) : null, l => l.HasValue ? l.Value / 100m : null);

    /// <summary>Converts an <see cref="Option{T}"/> of <see cref="string"/> to and from a nullable string.</summary>
    public static ValueConverter<Option<string>, string?> OptionStringToNullableString { get; } =
        new(opt => opt.Match<string?>(v => v, () => null),
            str => str != null ? Option.Some(str) : Option.None<string>());

    /// <summary>Converts an <see cref="Option{T}"/> of <see cref="DateTimeOffset"/> to and from nullable UTC ticks.</summary>
    public static ValueConverter<Option<DateTimeOffset>, long?> OptionDateTimeOffsetToNullableTicks { get; } =
        new(opt => opt.Match<long?>(v => v.ToUniversalTime().UtcTicks, () => null),
            ticks => ticks.HasValue ? Option.Some(new DateTimeOffset(ticks.Value, TimeSpan.Zero)) : Option.None<DateTimeOffset>());

    /// <summary>Converts an <see cref="Option{T}"/> of <see cref="ConflictPolicy"/> to and from a nullable integer.</summary>
    public static ValueConverter<Option<ConflictPolicy>, int?> OptionConflictPolicyToNullableInt { get; } =
        new(opt => opt.Match<int?>(v => (int)v, () => null),
            value => value.HasValue ? Option.Some((ConflictPolicy)value.Value) : Option.None<ConflictPolicy>());
}
