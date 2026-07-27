using System.Text.RegularExpressions;

namespace Fab4Kids.Web.Catalogue;

/// <summary>Converts catalogue display names to URL slugs, mirroring the previous Astro site's <c>toSlug</c> helper.</summary>
public static partial class SlugExtensions
{
    public static string ToSlug(this string value) => WhitespaceRun().Replace(value.Trim().ToLowerInvariant(), "-");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
