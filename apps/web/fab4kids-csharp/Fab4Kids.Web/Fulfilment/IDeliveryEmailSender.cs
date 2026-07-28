using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Sends the PDF download-link delivery email to a customer.</summary>
public interface IDeliveryEmailSender
{
    Task<Result<UnitFp, string>> SendAsync(string toAddress, string orderReference, IReadOnlyList<DeliveryLink> links, CancellationToken cancellationToken);
}
