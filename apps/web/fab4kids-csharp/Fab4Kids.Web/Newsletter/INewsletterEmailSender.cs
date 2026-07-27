using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <summary>Sends the welcome/confirmation email for a newly stored newsletter subscriber.</summary>
public interface INewsletterEmailSender
{
    Task<Result<UnitFp, string>> SendAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken);
}
