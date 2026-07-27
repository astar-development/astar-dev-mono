using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <summary>Checks and records newsletter subscriber email addresses.</summary>
public interface INewsletterSubscriberStore
{
    Task<Result<bool, string>> ExistsAsync(string email, CancellationToken cancellationToken);

    Task<Result<UnitFp, string>> AddAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken);
}
