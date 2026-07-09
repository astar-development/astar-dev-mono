namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The outcome of evaluating an image's tags against the <see cref="TagRules" />.</summary>
public abstract record TagOutcome;

/// <summary>The image should be skipped because one of its tags matched an ignore-completely rule.</summary>
/// <param name="Tags">The tag text scraped from the image page before the skip was detected.</param>
public sealed record SkipImage(IReadOnlyList<string> Tags) : TagOutcome;

/// <summary>The image should be kept, with the derived file prefix and directory segments to save it under.</summary>
/// <param name="FilePrefix">The prefix to apply to the saved file name.</param>
/// <param name="DirectorySegments">The directory segments the image should be saved under.</param>
/// <param name="Tags">The tag text scraped from the image page.</param>
public sealed record Accept(string FilePrefix, IReadOnlyList<string> DirectorySegments, IReadOnlyList<string> Tags) : TagOutcome;
