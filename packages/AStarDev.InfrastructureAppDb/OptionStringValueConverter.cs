using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

/// <summary>Converts an <see cref="Option{T}"/> of <see cref="string"/> to and from a nullable string, including NULL column values which map to <c>Option.None</c>.</summary>
public sealed class OptionStringValueConverter() : ValueConverter<Option<string>, string?>(opt => opt.Match<string?>(v => v, () => null), str => str != null ? Option.Some(str) : Option.None<string>())
{
    /// <inheritdoc />
    public override bool ConvertsNulls => true;
}
