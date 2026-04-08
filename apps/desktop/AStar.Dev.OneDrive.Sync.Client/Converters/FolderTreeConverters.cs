using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Converts tree depth (int) to left-margin indentation width.</summary>
public sealed class DepthToIndentConverter : IValueConverter
{
    public static readonly DepthToIndentConverter Instance = new();
    private const double IndentWidth = 16.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int depth ? depth * IndentWidth : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
