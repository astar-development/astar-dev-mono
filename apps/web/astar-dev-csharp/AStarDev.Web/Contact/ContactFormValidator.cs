using System.Text.RegularExpressions;
using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Contact;

/// <summary>Validates raw contact-form input, accumulating every field error rather than stopping at the first.</summary>
public static class ContactFormValidator
{
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public static Validation<ContactMessage> Validate(string name, string email, string message, bool sendCopy)
    {
        Validation<string>[] validations = [ValidateName(name), ValidateEmail(email), ValidateMessage(message)];

        return validations.Combine().Match(
            values => Validation.Valid(ContactMessageFactory.Create(values[0], values[1], values[2], sendCopy)),
            errors => Validation.Invalid<ContactMessage>(errors));
    }

    private static Validation<string> ValidateName(string name)
    {
        string trimmed = name.Trim();

        if (trimmed.Length == 0)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("name", "Name is required."));

        if (trimmed.Length > 200)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("name", "Name must be 200 characters or fewer."));

        return Validation.Valid(trimmed);
    }

    private static Validation<string> ValidateEmail(string email)
    {
        string trimmed = email.Trim();

        if (trimmed.Length == 0)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("email", "Email is required."));

        if (!EmailPattern.IsMatch(trimmed))
            return Validation.Invalid<string>(ValidationErrorFactory.Create("email", "Please enter a valid email address."));

        return Validation.Valid(trimmed);
    }

    private static Validation<string> ValidateMessage(string message)
    {
        string trimmed = message.Trim();

        if (trimmed.Length == 0)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("message", "Message is required."));

        if (trimmed.Length < 10)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("message", "Message must be at least 10 characters."));

        if (trimmed.Length > 5000)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("message", "Message must be 5000 characters or fewer."));

        return Validation.Valid(trimmed);
    }
}
