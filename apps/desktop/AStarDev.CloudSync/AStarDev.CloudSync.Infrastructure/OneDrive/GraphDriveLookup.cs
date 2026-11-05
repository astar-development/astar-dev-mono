using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Reads the drive through the Microsoft Graph SDK.</summary>
/// <param name="httpClientFactory">Creates the HTTP client for Graph.</param>
/// <param name="logger">Reports lookup failures, without tokens, Graph error messages or account details.</param>
public sealed class GraphDriveLookup(IHttpClientFactory httpClientFactory, ILogger<GraphDriveLookup> logger) : IDriveLookup
{
    /// <summary>The name of the HTTP client used for Graph.</summary>
    public const string HttpClientName = "MicrosoftGraph";

    /// <inheritdoc />
    public Task<Result<MicrosoftDrive, AccountAddFailure>> GetDriveAsync(string accessToken, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadDriveAsync(accessToken, cancellationToken), cancellationToken)
            .MatchAsync(ToDrive, LookupFailed);

    private async Task<Drive?> ReadDriveAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var graph = new GraphServiceClient(httpClientFactory.CreateClient(HttpClientName), new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

        return await graph.Me.Drive.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Result<MicrosoftDrive, AccountAddFailure> ToDrive(Drive? drive) =>
        DriveIdFactory.Create(drive?.Id).Match(
            driveId => Result.Success<MicrosoftDrive, AccountAddFailure>(MicrosoftDriveFactory.Create(driveId, StorageUsageFactory.Create(drive?.Quota?.Used ?? 0, drive?.Quota?.Total ?? 0))),
            _ => Failed("Graph returned a drive without an id", 200, "none", AccountAddFailureFactory.CreateDriveUnavailable()));

    private Result<MicrosoftDrive, AccountAddFailure> LookupFailed(Exception exception) => exception switch
    {
        HttpRequestException => Failed("Graph could not be reached", 0, exception.GetType().Name, AccountAddFailureFactory.CreateNetwork()),
        ApiException { ResponseStatusCode: 401 or 403 } apiException => Failed("Graph refused the access token", apiException.ResponseStatusCode, exception.GetType().Name, AccountAddFailureFactory.CreateAuthRejected()),
        ApiException apiException => Failed("Graph returned an error", apiException.ResponseStatusCode, exception.GetType().Name, AccountAddFailureFactory.CreateDriveUnavailable()),
        _ => Failed("unexpected error", 0, exception.GetType().Name, AccountAddFailureFactory.CreateDriveUnavailable()),
    };

    private Result<MicrosoftDrive, AccountAddFailure> Failed(string reason, int statusCode, string exceptionType, AccountAddFailure failure)
    {
        AccountAddLog.DriveLookupFailed(logger, reason, statusCode, exceptionType);

        return Result.Failure<MicrosoftDrive, AccountAddFailure>(failure);
    }
}
