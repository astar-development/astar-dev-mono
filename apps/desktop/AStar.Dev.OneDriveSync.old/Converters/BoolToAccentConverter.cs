using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>Returns the accent colour when <c>true</c>, transparent when <c>false</c>.</summary>
public sealed class BoolToAccentConverter : IValueConverter
{
    private static readonly Color Accent      = Color.Parse("#185FA5");
    private static readonly Color Transparent = Colors.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Accent : Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
