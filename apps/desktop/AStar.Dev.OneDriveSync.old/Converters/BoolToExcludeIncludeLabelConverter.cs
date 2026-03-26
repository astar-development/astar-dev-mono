using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>
/// Converts <c>IsIncluded</c> (bool) to a button label.
/// <c>true</c> (currently included) → "Exclude"; <c>false</c> (currently excluded) → "Include".
/// </summary>
public sealed class BoolToExcludeIncludeLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Exclude" : "Include";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
