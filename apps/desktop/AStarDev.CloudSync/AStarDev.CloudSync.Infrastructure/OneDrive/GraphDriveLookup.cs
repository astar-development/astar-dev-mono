using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Reads the drive through the Microsoft Graph SDK.</summary>
/// <param name="httpClientFactory">Creates the HTTP client for Graph.</param>
/// <param name="logger">Reports lookup failures, without tokens or account details.</param>
public sealed class GraphDriveLookup(IHttpClientFactory httpClientFactory, ILogger<GraphDriveLookup> logger) : IDriveLookup
{
    /// <summary>The name of the HTTP client used for Graph.</summary>
    public const string HttpClientName = "MicrosoftGraph";

    /// <inheritdoc />
    public Task<Result<MicrosoftDrive, AccountAddFailure>> GetDriveAsync(string accessToken, CancellationToken cancellationToken) => throw new NotImplementedException($"{httpClientFactory}{logger}{accessToken.Length}{cancellationToken}");
}
