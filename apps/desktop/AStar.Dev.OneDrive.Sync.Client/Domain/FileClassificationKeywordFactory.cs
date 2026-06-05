using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="FileClassificationKeyword"/>.</summary>
public static class FileClassificationKeywordFactory
{
    /// <summary>Creates a <see cref="FileClassificationKeyword"/> with validation.</summary>
    public static Result<FileClassificationKeyword, string> Create(string value, Option<bool> isSpecialOverride)
    {
        string normalised = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if(string.IsNullOrEmpty(normalised))
            return new Result<FileClassificationKeyword, string>.Error("Value must not be empty.");

        return new Result<FileClassificationKeyword, string>.Ok(new(normalised, isSpecialOverride));
    }
}
