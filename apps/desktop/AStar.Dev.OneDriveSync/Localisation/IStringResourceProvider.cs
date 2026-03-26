using System.Globalization;

namespace AStar.Dev.OneDriveSync.Localisation;

public interface IStringResourceProvider
{
    string? GetString(string key, CultureInfo culture);
}
