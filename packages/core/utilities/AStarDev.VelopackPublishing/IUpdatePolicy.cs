namespace AStar.Dev.Velopack.Publishing;

/// <summary>Decides what to do about an update check result and the user's earlier choices (#1412). It is pure: the same inputs always give the same action.</summary>
public interface IUpdatePolicy
{
    /// <summary>Decides. A found update with no deferral prompts. A deferred update prompts again after one hour and is forced after seven days. A newer version than the deferred one starts again. Nothing is done when there is no update.</summary>
    /// <param name="result">What the update check found.</param>
    /// <param name="deferral">What the user did about the update that was found before.</param>
    /// <param name="now">The current time.</param>
    UpdateAction Decide(UpdateCheckResult result, UpdateDeferral deferral, DateTimeOffset now);
}
