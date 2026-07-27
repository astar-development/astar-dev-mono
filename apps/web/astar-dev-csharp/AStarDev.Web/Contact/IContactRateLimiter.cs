namespace AStarDev.Web.Contact;

/// <summary>Limits how often a single IP address may submit the contact form.</summary>
public interface IContactRateLimiter
{
    /// <summary>Returns <see langword="true"/> if the request is within budget, <see langword="false"/> if it should be rejected.</summary>
    bool TryAcquire(string ipAddress);
}
