using AStar.Dev.Functional.Extensions;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="FileClassificationKeyword"/>.</summary>
public static class FileClassificationKeywordFactory
{
    /// <summary>Creates a <see cref="FileClassificationKeyword"/> with validation.</summary>
    public static Result<FileClassificationKeyword, string> Create(string value, Option<bool> isFamous, Option<bool> isInternet)
    {
        string normalised = value?.Trim().ToTitleCase() ?? string.Empty;
        if(string.IsNullOrEmpty(normalised))
            return new Result<FileClassificationKeyword, string>.Error("Value must not be empty.");

        return new Result<FileClassificationKeyword, string>.Ok(new(normalised, isFamous, isInternet));
    }
}
