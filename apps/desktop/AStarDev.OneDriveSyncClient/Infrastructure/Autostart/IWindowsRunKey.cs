namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Writes values to the current user's Windows Run registry key.</summary>
public interface IWindowsRunKey
{
    /// <summary>Sets the named value to the command launched at user logon.</summary>
    void SetValue(string valueName, string command);
}
