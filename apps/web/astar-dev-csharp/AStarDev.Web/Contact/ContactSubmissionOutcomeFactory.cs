using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Contact;

/// <summary>Factory for the <see cref="ContactSubmissionOutcome"/> discriminated union.</summary>
public static class ContactSubmissionOutcomeFactory
{
    public static ContactSubmissionOutcome Succeeded() => new ContactSubmissionSucceeded();

    public static ContactSubmissionOutcome RateLimited() => new ContactSubmissionRateLimited();

    public static ContactSubmissionOutcome ValidationFailed(IReadOnlyList<ValidationError> errors) => new ContactSubmissionValidationFailed(errors);

    public static ContactSubmissionOutcome SendFailed(string message) => new ContactSubmissionSendFailed(message);
}
