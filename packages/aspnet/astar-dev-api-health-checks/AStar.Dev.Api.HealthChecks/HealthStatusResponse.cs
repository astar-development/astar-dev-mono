namespace AStar.Dev.Api.HealthChecks;

/// <summary>
/// </summary>
public sealed class HealthStatusResponse
{
    /// <summary>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public string? Description { get; set; } = "Unable to retrieve the description of the Health Status";

    /// <summary>
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public double DurationInMilliseconds { get; set; }

    /// <summary>
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; set; }

    /// <summary>
    /// </summary>
    public string? Exception { get; set; } = string.Empty;
}
