namespace AStarDev.Web.Contact;

/// <summary>Factory for <see cref="ContactMessage"/>.</summary>
public static class ContactMessageFactory
{
    public static ContactMessage Create(string name, string email, string message, bool sendCopy) => new(name, email, message, sendCopy);
}
