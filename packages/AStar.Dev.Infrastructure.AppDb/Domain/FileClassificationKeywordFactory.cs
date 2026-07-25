using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="FileClassificationKeyword"/>.</summary>
public static class FileClassificationKeywordFactory
{
    /// <summary>Creates a <see cref="FileClassificationKeyword"/> with validation.</summary>
    public static Result<FileClassificationKeyword, string> Create(string value, Option<bool> isFamous, Option<bool> isInternet)
    {
        string normalised = value?.Trim().ToTitleCase() ?? string.Empty;
        if (string.IsNullOrEmpty(normalised))
            return new Fail<FileClassificationKeyword, string>("Value must not be empty.");

        return new Ok<FileClassificationKeyword, string>(new(normalised, isFamous, isInternet));
    }
}
