using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

public static class SqliteTypeConverters
{
    public static ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicks { get; } =
        new(dto => dto.ToUniversalTime().UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public static ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToTicks { get; } =
        new(dto => dto.HasValue ? dto.Value.ToUniversalTime().UtcTicks : null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

    public static ValueConverter<TimeSpan, long> TimeSpanToTicks { get; } =
        new(ts => ts.Ticks, ticks => TimeSpan.FromTicks(ticks));

    public static ValueConverter<TimeSpan?, long?> NullableTimeSpanToTicks { get; } =
        new(ts => ts.HasValue ? ts.Value.Ticks : null, ticks => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : null);

    public static ValueConverter<Guid, byte[]> GuidToBytes { get; } =
        new(g => g.ToByteArray(), b => new Guid(b));

    public static ValueConverter<Guid?, byte[]?> NullableGuidToBytes { get; } =
        new(g => g.HasValue ? g.Value.ToByteArray() : null, b => b != null ? new Guid(b) : null);

    public static ValueConverter<decimal, long> DecimalToCents { get; } =
        new(d => (long)Math.Round(d * 100m), l => l / 100m);

    public static ValueConverter<decimal?, long?> NullableDecimalToCents { get; } =
        new(d => d.HasValue ? (long?)Math.Round(d.Value * 100m) : null, l => l.HasValue ? l.Value / 100m : null);

    public static ValueConverter<Option<string>, string?> OptionStringToNullableString { get; } =
        new(opt => opt.Match<string?>(v => v, () => null),
            str => str != null ? Option.Some(str) : Option.None<string>());

    public static ValueConverter<Option<DateTimeOffset>, long?> OptionDateTimeOffsetToNullableTicks { get; } =
        new(opt => opt.Match<long?>(v => v.ToUniversalTime().UtcTicks, () => null),
            ticks => ticks.HasValue ? Option.Some(new DateTimeOffset(ticks.Value, TimeSpan.Zero)) : Option.None<DateTimeOffset>());

    public static ValueConverter<Option<ConflictPolicy>, int?> OptionConflictPolicyToNullableInt { get; } =
        new(opt => opt.Match<int?>(v => (int)v, () => null),
            value => value.HasValue ? Option.Some((ConflictPolicy)value.Value) : Option.None<ConflictPolicy>());
}
