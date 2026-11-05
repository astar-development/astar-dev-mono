using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
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
            _ => Failed("Graph returned a drive without an id.", AccountAddFailureFactory.CreateDriveUnavailable()));

    private Result<MicrosoftDrive, AccountAddFailure> LookupFailed(Exception exception) => exception switch
    {
        HttpRequestException => Failed($"The drive could not be read because Graph could not be reached ({exception.GetType().Name}).", AccountAddFailureFactory.CreateNetwork()),
        ApiException apiException => Failed($"Graph returned HTTP {apiException.ResponseStatusCode} for the drive ({exception.GetType().Name}).", AccountAddFailureFactory.CreateDriveUnavailable()),
        _ => Failed($"The drive could not be read ({exception.GetType().Name}).", AccountAddFailureFactory.CreateDriveUnavailable()),
    };

    private Result<MicrosoftDrive, AccountAddFailure> Failed(string message, AccountAddFailure failure)
    {
        LogMessage.Warning(logger, nameof(GraphDriveLookup), message);

        return Result.Failure<MicrosoftDrive, AccountAddFailure>(failure);
    }
}
