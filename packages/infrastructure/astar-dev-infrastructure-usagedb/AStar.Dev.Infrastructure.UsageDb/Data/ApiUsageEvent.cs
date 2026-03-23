namespace AStar.Dev.Infrastructure.UsageDb.Data;

/// <summary>
/// </summary>
/// <param name="ApiName"></param>
/// <param name="ApiEndpoint"></param>
/// <param name="HttpMethod"></param>
/// <param name="ElapsedMilliseconds"></param>
/// <param name="StatusCode"></param>
public record ApiUsageEvent(string ApiName, string ApiEndpoint, string HttpMethod, double ElapsedMilliseconds, int StatusCode)
{
    /// <summary>
    /// </summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}