using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <inheritdoc />
internal sealed class DeltaQueryService(IGraphClientFactory graphClientFactory, ILogger<DeltaQueryService> logger) : IDeltaQueryService
{
    private const string MoveOrRenamedAnnotation = "@microsoft.graph.moveOrRenamed";

    /// <inheritdoc />
    public async Task<Result<DeltaQueryResult, DeltaQueryError>> GetDeltaAsync(string accessToken, string folderId, string? deltaToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        using var client = graphClientFactory.Create(accessToken);

        try
        {
            var drive = await CallWithRetryAsync(() => client.Me.Drive.GetAsync(cancellationToken: ct), ct).ConfigureAwait(false);

            if (drive?.Id is null)
                return new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.Failed("Could not resolve OneDrive drive ID for the account."));

            var allItems = new List<DriveItem>();
            string? nextDeltaLink = null;
            var isFullSync = deltaToken is null;

            var firstPageBuilder = deltaToken is null
                ? client.Drives[drive.Id].Items[folderId].Delta
                : client.Drives[drive.Id].Items[folderId].Delta.WithUrl(deltaToken);

            var currentPage = await CallWithRetryAsync(() => firstPageBuilder.GetAsDeltaGetResponseAsync(cancellationToken: ct), ct).ConfigureAwait(false);

            while (currentPage is not null)
            {
                if (currentPage.Value is not null)
                    allItems.AddRange(currentPage.Value);

                if (currentPage.OdataDeltaLink is not null)
                {
                    nextDeltaLink = currentPage.OdataDeltaLink;
                    break;
                }

                if (currentPage.OdataNextLink is null)
                    break;

                var nextLink = currentPage.OdataNextLink;
                currentPage = await CallWithRetryAsync(() => client.Drives[drive.Id].Items[folderId].Delta.WithUrl(nextLink).GetAsDeltaGetResponseAsync(cancellationToken: ct), ct).ConfigureAwait(false);
            }

            if (nextDeltaLink is null)
                return new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.Failed("Graph delta response did not include a deltaLink."));

            return new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(MapItems(allItems), nextDeltaLink, isFullSync));
        }
        catch (ODataError oDataError) when (oDataError.ResponseStatusCode == 410)
        {
            return new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.TokenExpired());
        }
        catch (ODataError oDataError) when (oDataError.ResponseStatusCode == 429)
        {
            return new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.Throttled("Graph API throttled after maximum retries."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.Failed(ex.Message));
        }
    }

    private Task<T> CallWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken ct)
        => GraphRetryHelper.CallWithRetryAsync(operation, logger, ct);

    private static IReadOnlyList<DeltaItem> MapItems(IEnumerable<DriveItem> items)
        => [..items.Select(MapItem)];

    private static DeltaItem MapItem(DriveItem item)
    {
        if (item.Deleted is not null)
            return DeltaItemFactory.CreateDeleted(item.Id!);

        var isMoveOrRenamed = item.AdditionalData?.ContainsKey(MoveOrRenamedAnnotation) == true;

        if (item.Folder is not null && isMoveOrRenamed)
        {
            var previousName = item.AdditionalData?.TryGetValue("previousParentReference", out var prev) == true
                ? prev?.ToString()
                : null;

            return DeltaItemFactory.Create(item.Id!, item.Name!, item.ParentReference?.Id, DeltaItemType.FolderRenamed, previousName);
        }

        var itemType = item.Folder is not null ? DeltaItemType.Folder : DeltaItemType.File;

        return DeltaItemFactory.Create(item.Id!, item.Name!, item.ParentReference?.Id, itemType);
    }
}
