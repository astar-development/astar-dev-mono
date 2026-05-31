using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

/// <summary>Converts bool to accent colour (active) or muted (inactive) — used for tab indicators.</summary>
public sealed class BoolToAccentConverter : IValueConverter
{
    public static readonly BoolToAccentConverter Instance = new();
    private static readonly Color Active   = Color.Parse("#185FA5");
    private static readonly Color Inactive = Color.Parse("#D3D1C7");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Active : Inactive;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when a string is non-null and non-empty.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

