using Microsoft.EntityFrameworkCore;

namespace AStarDev.FilesDb;

/// <summary>
/// Represents the Entity Framework database context for managing file entities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FilesDbContext"/> class with the specified options.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet of <see cref="FileEntity"/> representing the files in the database.
    /// </summary>
    public DbSet<FileEntity> Files => Set<FileEntity>();
}
