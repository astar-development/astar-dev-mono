using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class FileClassificationRuleRepository(IDbContextFactory<AppDbContext> dbFactory) : IFileClassificationRuleRepository
{
    public async Task<IReadOnlyList<FileClassificationRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entities = await db.FileClassificationRules.AsNoTracking().ToListAsync(cancellationToken);

        return entities
            .Select(e => FileClassificationRuleFactory.Create(
                e.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries),
                FileClassificationFactory.Create(
                    e.Level1,
                    e.Level2 is not null ? Option.Some(e.Level2) : Option.None<string>(),
                    e.Level3 is not null ? Option.Some(e.Level3) : Option.None<string>(),
                    e.IsSpecial)))
            .ToList()
            .AsReadOnly();
    }
}
