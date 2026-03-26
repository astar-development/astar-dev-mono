using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.Converters;

/// <summary>Returns a collapse glyph (▲) when <c>true</c>, an expand glyph (▼) when <c>false</c>.</summary>
public sealed class BoolToCollapseExpandConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "▲" : "▼";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
