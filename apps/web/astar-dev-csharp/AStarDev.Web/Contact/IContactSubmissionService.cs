namespace AStarDev.Web.Contact;

/// <summary>Orchestrates a contact-form submission: anti-spam checks, validation, then sending.</summary>
public interface IContactSubmissionService
{
    Task<ContactSubmissionOutcome> SubmitAsync(string name, string email, string message, bool sendCopy, string website, string ipAddress, CancellationToken cancellationToken);
}
