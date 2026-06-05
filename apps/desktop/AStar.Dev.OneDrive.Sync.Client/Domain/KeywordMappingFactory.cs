using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="KeywordMapping"/>.</summary>
public static class KeywordMappingFactory
{
    /// <summary>Creates a <see cref="KeywordMapping"/> from the supplied values, returning an error if keyword or level1 are null, empty, or whitespace. The keyword is trimmed before storing.</summary>
    public static Result<KeywordMapping, string> Create(string keyword, string level1, Option<string> level2, Option<string> level3, bool isSpecial)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new Result<KeywordMapping, string>.Error("Keyword must not be empty.");

        if (string.IsNullOrWhiteSpace(level1))
            return new Result<KeywordMapping, string>.Error("Level1 must not be empty.");

        return new Result<KeywordMapping, string>.Ok(new KeywordMapping(keyword.Trim(), level1, level2, level3, isSpecial));
    }
}
