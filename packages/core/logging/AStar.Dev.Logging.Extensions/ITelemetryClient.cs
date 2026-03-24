namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Defines the contract for telemetry client implementations used to track and log telemetry data.
/// </summary>
public interface ITelemetryClient
{
    /// <summary>
    ///     Tracks a page view with the specified name.
    /// </summary>
    /// <param name="name">The name of the page being viewed.</param>
    void TrackPageView(string name);
}