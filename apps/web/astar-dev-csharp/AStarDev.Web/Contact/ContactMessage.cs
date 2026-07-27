namespace AStarDev.Web.Contact;

/// <summary>A validated contact-form submission, ready to send.</summary>
public sealed record ContactMessage(string Name, string Email, string Message, bool SendCopy);
