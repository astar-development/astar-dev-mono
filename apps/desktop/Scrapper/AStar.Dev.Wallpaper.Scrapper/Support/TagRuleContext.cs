using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The configuration <see cref="TagRules" /> needs to evaluate an image's tags.</summary>
/// <param name="InitialDirectory">The directory segment the image is saved under before any tag rule is applied.</param>
/// <param name="BaseDirectoryFamous">The directory segment added when a famous-person tag rule matches.</param>
/// <param name="TagsToIgnoreCompletely">The tags/categories that cause an image to be skipped entirely.</param>
/// <param name="TagsTextToIgnore">The tag text that should not contribute to the derived file prefix.</param>
public record TagRuleContext(string InitialDirectory, string BaseDirectoryFamous, TagsToIgnoreCompletely TagsToIgnoreCompletely, TagsTextToIgnore TagsTextToIgnore);
