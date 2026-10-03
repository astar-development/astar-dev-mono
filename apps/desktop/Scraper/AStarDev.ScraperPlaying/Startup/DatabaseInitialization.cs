using AStarDev.ControlDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Startup;

/// <summary>
/// Runs the startup database migration off the calling thread, once, so the main window can appear immediately and wait for
/// <see cref="ReadyAsync"/> before it touches the database. A migration failure is surfaced through the returned task.
/// </summary>
/// <param name="dbContextFactory">The factory used to create the <see cref="ControlDbContext"/> the migration runs against.</param>
/// <param name="logger">The logger used to report migration progress and failures.</param>
public sealed class DatabaseInitialization(IDbContextFactory<ControlDbContext> dbContextFactory, ILogger<DatabaseInitialization> logger) : IDatabaseInitialization
{
    private readonly Lazy<Task> ready = new(() => Task.Run(() => DatabaseMigrator.MigrateAsync(dbContextFactory, logger)));

    /// <inheritdoc/>
    public Task ReadyAsync() => ready.Value;
}
