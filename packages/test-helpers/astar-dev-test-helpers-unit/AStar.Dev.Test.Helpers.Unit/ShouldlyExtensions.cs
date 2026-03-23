using AStar.Dev.Utilities;
using Shouldly;

namespace AStar.Dev.Test.Helpers.Unit;

/// <summary>
///     The <see cref="ShouldlyExtensions" /> class containing the current extensions for the Shouldly package
/// </summary>
public static class ShouldlyExtensions
{
    /// <summary>
    ///     The ShouldMatchMaskedApproved method abstracts the original ShouldMatchApproved and adds masking for ISO Dates and GUIDs within the object being compared
    /// </summary>
    /// <param name="source">The object to check to the approved version</param>
    /// <typeparam name="T">The type of the object being compared</typeparam>
    public static void ShouldMatchMaskedApproved<T>(this T source)
        => source.ToJson().ShouldMatchApproved(options
                                                   => options
                                                      .WithScrubber(input => ShouldlyExtensionsRegex.RegexIsoDate().Replace(input, "<date>"))
                                                      .WithScrubber(input => ShouldlyExtensionsRegex.RegexGuid().Replace(input, "<GUID>")));
}