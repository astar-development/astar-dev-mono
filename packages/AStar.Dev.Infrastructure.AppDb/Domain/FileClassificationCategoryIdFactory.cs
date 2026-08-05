namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory methods for creating <see cref="FileClassificationCategoryId"/> instances.</summary>
public static class FileClassificationCategoryIdFactory
{
    /// <summary>Converts an integer to a <see cref="FileClassificationCategoryId"/>.</summary>
    /// <param name="id">The integer identifier.</param>
    /// <returns>A <see cref="FileClassificationCategoryId"/> instance.</returns>
    public static FileClassificationCategoryId Create(this int id) => new(id);
}
