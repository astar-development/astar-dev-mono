using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.Converters;

/// <summary>Returns <c>true</c> when the integer value is zero (used to show empty-state placeholders).</summary>
public sealed class IntZeroToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int n && n == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
