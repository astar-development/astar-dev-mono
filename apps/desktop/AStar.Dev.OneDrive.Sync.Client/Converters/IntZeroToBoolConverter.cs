using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Returns true when an integer equals zero — used for empty-state visibility.</summary>
public sealed class IntZeroToBoolConverter : IValueConverter
{
    public static readonly IntZeroToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isZero = value is int n && n == 0;

        return parameter is string s && s == "negate" ? !isZero : isZero;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
