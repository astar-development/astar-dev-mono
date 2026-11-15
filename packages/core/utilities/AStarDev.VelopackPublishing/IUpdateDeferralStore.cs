namespace AStar.Dev.Velopack.Publishing;

/// <summary>Remembers that the user deferred an update, so the choice survives a restart (#1412).</summary>
public interface IUpdateDeferralStore
{
    /// <summary>Loads what the user deferred. Nothing saved, or a file that cannot be read, means nothing is deferred.</summary>
    UpdateDeferral Load();

    /// <summary>Saves the deferral. <see cref="NotDeferred"/> removes it. A failure to save is logged, never thrown: the worst outcome is being asked again.</summary>
    /// <param name="deferral">What to remember.</param>
    void Save(UpdateDeferral deferral);
}
