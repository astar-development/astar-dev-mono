namespace AStar.Dev.Api.HealthChecks;

/// <summary>
///     The <see href="IApiClient"></see> interface.
/// </summary>
public interface IApiClient
{
    /// <summary>
    ///     The GetHealthAsync method will return the basic Health Status of the API.
    /// </summary>
    /// <param name="cancellationToken">The token to optionally use to cancel the operation</param>
    /// <returns>
    ///     An instance of the <see href="HealthStatusResponse"></see> class containing the text representation of the API
    ///     Health Status.
    /// </returns>
    public Task<HealthStatusResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
