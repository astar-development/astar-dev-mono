using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

/// <summary>Converts an <see cref="Option{T}"/> of <see cref="DateTimeOffset"/> to and from nullable UTC ticks, including NULL column values which map to <c>Option.None</c>.</summary>
public sealed class OptionDateTimeOffsetValueConverter() : ValueConverter<Option<DateTimeOffset>, long?>(opt => opt.Match<long?>(v => v.ToUniversalTime().UtcTicks, () => null), ticks => ticks.HasValue ? Option.Some(new DateTimeOffset(ticks.Value, TimeSpan.Zero)) : Option.None<DateTimeOffset>())
{
    /// <inheritdoc />
    public override bool ConvertsNulls => true;
}
