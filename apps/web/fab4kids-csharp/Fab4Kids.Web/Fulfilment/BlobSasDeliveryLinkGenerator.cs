using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.Fulfilment;

/// <inheritdoc cref="IPdfDeliveryLinkGenerator"/>
public sealed class BlobSasDeliveryLinkGenerator(ILogger<BlobSasDeliveryLinkGenerator> logger, BlobContainerClient? containerClient = null) : IPdfDeliveryLinkGenerator
{
    private static readonly TimeSpan SignedUrlTtl = TimeSpan.FromMinutes(15);

    public Task<Result<string, string>> GenerateSignedUrlAsync(string blobPath, CancellationToken cancellationToken)
    {
        if (containerClient is null || !containerClient.CanGenerateSasUri)
        {
            LogMessage.Error(logger, "Blob storage is not configured (missing connection string or container name).");

            return Task.FromResult(Result.Failure<string, string>("Something went wrong generating your download link."));
        }

        return Try.RunAsync(() => Task.FromResult(BuildSignedUrl(containerClient, blobPath))).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(BlobSasDeliveryLinkGenerator), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong generating your download link.";
        });
    }

    private static string BuildSignedUrl(BlobContainerClient containerClient, string blobPath)
    {
        var blobClient = containerClient.GetBlobClient(blobPath);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerClient.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(SignedUrlTtl)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
