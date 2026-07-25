using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="KeywordMapping"/>.</summary>
public static class KeywordMappingFactory
{
    /// <summary>Creates a <see cref="KeywordMapping"/> from the supplied values, returning an error if keyword or level1 are null, empty, or whitespace. The keyword is trimmed before storing.</summary>
    public static Result<KeywordMapping, string> Create(string keyword, string level1, Option<string> level2, Option<string> level3, bool isSpecial)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new Fail<KeywordMapping, string>("Keyword must not be empty.");

        if (string.IsNullOrWhiteSpace(level1))
            return new Fail<KeywordMapping, string>("Level1 must not be empty.");

        return new Ok<KeywordMapping, string>(new KeywordMapping(keyword.Trim(), level1, level2, level3, isSpecial));
    }
}
