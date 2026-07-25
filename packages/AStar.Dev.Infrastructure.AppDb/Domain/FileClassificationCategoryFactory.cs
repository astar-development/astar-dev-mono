using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="FileClassificationCategory"/>.</summary>
public static class FileClassificationCategoryFactory
{
    /// <summary>Creates a <see cref="FileClassificationCategory"/> with validation.</summary>
    public static Result<FileClassificationCategory, string> Create(FileClassificationCategoryId id, string name, int level, bool IsFamous, bool IsInternet, Option<FileClassificationCategoryId> parentId, bool IncludeInSearch)
    {
        string trimmedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmedName))
            return new Fail<FileClassificationCategory, string>("Name must not be empty.");

        if (level is < 1 or > 3)
            return new Fail<FileClassificationCategory, string>("Level must be 1, 2, or 3.");

        if (level == 1 && parentId is Option<FileClassificationCategoryId>.Some)
            return new Fail<FileClassificationCategory, string>("Level 1 category must not have a parent.");

        if (level is 2 or 3 && parentId is Option<FileClassificationCategoryId>.None)
            return new Fail<FileClassificationCategory, string>("Level 2 and 3 categories must have a parent.");

        return new Ok<FileClassificationCategory, string>(new(id, trimmedName, level, IsFamous, IsInternet, parentId, IncludeInSearch));
    }
}
