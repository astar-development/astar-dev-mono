namespace AStar.Dev.OneDriveSync.old.Localisation;

public interface ILocalisationService
{
    string Culture { get; }
    string GetString(string key);
}
