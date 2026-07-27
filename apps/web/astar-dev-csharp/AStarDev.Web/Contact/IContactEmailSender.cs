using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Contact;

/// <summary>Sends the owner notification (and optional sender copy) for a validated contact-form submission.</summary>
public interface IContactEmailSender
{
    Task<Result<UnitFp, string>> SendAsync(ContactMessage message, CancellationToken cancellationToken);
}
