using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Generates time-limited signed download URLs for purchased PDF resources.</summary>
public interface IPdfDeliveryLinkGenerator
{
    Task<Result<string, string>> GenerateSignedUrlAsync(string blobPath, CancellationToken cancellationToken);
}
