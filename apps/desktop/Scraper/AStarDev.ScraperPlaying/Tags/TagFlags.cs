namespace AStarDev.ScraperPlaying.Tags;

/// <summary>The flags of a tag that the user can change in the tags editor.</summary>
/// <param name="IgnoreImage">Whether any image carrying the tag is ignored.</param>
/// <param name="IsName">Whether the tag is a name, used to prefix the directory a wallpaper is saved under.</param>
/// <param name="IsFamous">Whether the tag is famous.</param>
public readonly record struct TagFlags(bool IgnoreImage, bool IsName, bool IsFamous);
