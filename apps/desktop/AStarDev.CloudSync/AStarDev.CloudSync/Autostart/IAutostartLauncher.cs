using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Tells how the installed app is started (#1410).</summary>
public interface IAutostartLauncher
{
    /// <summary>The full path of the program to start at login, or nothing for a build that is not installed, so a development build never registers itself.</summary>
    Option<string> LauncherPath { get; }
}
