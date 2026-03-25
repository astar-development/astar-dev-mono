namespace AStar.Dev.OneDriveSync.Localisation;

public interface ILocalisationService
{
    string Culture { get; }
    string GetString(string key);
}
