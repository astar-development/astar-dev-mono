using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>Returns <c>true</c> when the string is non-null and non-empty.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
