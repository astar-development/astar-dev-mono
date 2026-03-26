using System.Globalization;

namespace AStar.Dev.OneDriveSync.old.Localisation;

public interface IStringResourceProvider
{
    string? GetString(string key, CultureInfo culture);
}
