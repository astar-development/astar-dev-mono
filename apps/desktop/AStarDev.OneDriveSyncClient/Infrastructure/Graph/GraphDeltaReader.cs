using System.Text.RegularExpressions;
using System.Web;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.Graph.Models.ODataErrors;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>Reads the drive-root delta feed. Only the opaque token is taken from any link, and requests always go to the authenticated client's own base address, so a tampered link cannot redirect the bearer token.</summary>
internal sealed partial class GraphDeltaReader(IGraphClientFactory graphClientFactory)
{
    private const string RootItem = "root";
    private const string LatestToken = "latest";
    private const string TokenKey = "token";
    private const int GoneStatusCode = 410;

    internal Task<Result<string, string>> GetLatestDeltaLinkAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, CancellationToken cancellationToken)
        => GraphFailure.CaptureAsync(
            async () =>
            {
                var client = graphClientFactory.CreateClient(tokenFactory);
                var response = await client.Drives[driveId.Value].Items[RootItem].DeltaWithToken(LatestToken).GetAsDeltaWithTokenGetResponseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                return string.IsNullOrEmpty(response?.OdataDeltaLink)
                    ? new Fail<string, string>("Graph did not return a delta link")
                    : (Result<string, string>)new Ok<string, string>(response.OdataDeltaLink);
            },
            cancellationToken);

    internal Task<Result<DeltaQueryResult, string>> GetChangesAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string deltaLink, CancellationToken cancellationToken)
    {
        string? token = ExtractToken(deltaLink);

        if (token is null)
            return Task.FromResult<Result<DeltaQueryResult, string>>(new Ok<DeltaQueryResult, string>(DeltaQueryResultFactory.CreateResyncRequired()));

        return Try.RunAsync(() => ReadChangesAsync(tokenFactory, driveId, token, cancellationToken), cancellationToken)
            .MatchAsync(
                result => result,
                exception => exception is ODataError { ResponseStatusCode: GoneStatusCode }
                    ? new Ok<DeltaQueryResult, string>(DeltaQueryResultFactory.CreateResyncRequired())
                    : GraphFailure.ToFail<DeltaQueryResult>(exception));
    }

    private async Task<Result<DeltaQueryResult, string>> ReadChangesAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string firstToken, CancellationToken cancellationToken)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);
        List<DeltaChange> changes = [];
        string? nextDeltaLink = null;
        string? token = firstToken;

        while (token is not null)
        {
            string pageToken = token;
            var page = await client.Drives[driveId.Value].Items[RootItem].DeltaWithToken(pageToken).GetAsDeltaWithTokenGetResponseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            changes.AddRange((page?.Value ?? []).Where(item => item.Root is null).Select(MapChange));
            nextDeltaLink = page?.OdataDeltaLink;
            token = string.IsNullOrEmpty(nextDeltaLink) ? ExtractToken(page?.OdataNextLink) : null;
        }

        return string.IsNullOrEmpty(nextDeltaLink)
            ? new Fail<DeltaQueryResult, string>("Graph did not return a delta link")
            : new Ok<DeltaQueryResult, string>(DeltaQueryResultFactory.CreateChangesFound(changes, nextDeltaLink));
    }

    private static DeltaChange MapChange(Microsoft.Graph.Models.DriveItem item)
    {
        string itemId = item.Id ?? string.Empty;

        return item.Deleted is not null
            ? DeltaChangeFactory.CreateDeleted(itemId)
            : DeltaChangeFactory.CreateChanged(itemId, item.ParentReference?.Id is string parentId ? Option.Some(parentId) : Option.None<string>());
    }

    private static string? ExtractToken(string? link)
    {
        if (string.IsNullOrEmpty(link) || !Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return null;

        string? token = HttpUtility.ParseQueryString(uri.Query)[TokenKey] ?? ExtractPathToken(uri.AbsolutePath);

        return string.IsNullOrEmpty(token) ? null : token;
    }

    private static string? ExtractPathToken(string absolutePath)
    {
        var match = PathTokenPattern().Match(Uri.UnescapeDataString(absolutePath));

        return match.Success ? match.Groups[TokenKey].Value : null;
    }

    [GeneratedRegex("""delta\(token='(?<token>[^']+)'\)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PathTokenPattern();
}
