namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Supplies the environment details needed for startup registration.</summary>
public interface IAutostartContextProvider
{
    /// <summary>Returns the context for the running process.</summary>
    AutostartContext Current();
}
