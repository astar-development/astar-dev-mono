using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Contact;

/// <summary>The result of attempting to submit the contact form.</summary>
public abstract record ContactSubmissionOutcome;

/// <summary>The message was accepted (or silently dropped as spam) — the sender should see a success message.</summary>
public sealed record ContactSubmissionSucceeded : ContactSubmissionOutcome;

/// <summary>The sender has submitted the form too many times recently.</summary>
public sealed record ContactSubmissionRateLimited : ContactSubmissionOutcome;

/// <summary>One or more fields failed validation.</summary>
public sealed record ContactSubmissionValidationFailed(IReadOnlyList<ValidationError> Errors) : ContactSubmissionOutcome;

/// <summary>The message was valid but could not be sent.</summary>
public sealed record ContactSubmissionSendFailed(string Message) : ContactSubmissionOutcome;
