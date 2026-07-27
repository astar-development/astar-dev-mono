using System.Text.RegularExpressions;
using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <summary>Validates a raw newsletter signup email address, mirroring the previous Astro site's validation.</summary>
public static class NewsletterValidator
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public static Validation<string> Validate(string email)
    {
        var trimmed = email.Trim();

        if (trimmed.Length == 0)
            return Validation.Invalid<string>(ValidationErrorFactory.Create("email", "Email is required."));

        if (!EmailPattern.IsMatch(trimmed))
            return Validation.Invalid<string>(ValidationErrorFactory.Create("email", "Please enter a valid email address."));

        return Validation.Valid(trimmed);
    }
}
