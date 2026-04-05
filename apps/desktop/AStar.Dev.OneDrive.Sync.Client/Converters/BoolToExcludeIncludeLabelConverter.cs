using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Converts bool IsIncluded to "Exclude" / "Include" button label.</summary>
public sealed class BoolToExcludeIncludeLabelConverter : IValueConverter
{
    public static readonly BoolToExcludeIncludeLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Exclude" : "Include";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
