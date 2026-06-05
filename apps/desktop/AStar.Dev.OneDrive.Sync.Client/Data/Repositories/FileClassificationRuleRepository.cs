using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
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
                    .OrderBy(r => r.Classification.IsSpecial)
                    .ThenBy(r => r.Classification.Level1)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<FileClassificationRuleEntry>> GetAllWithIdsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entities = await db.FileClassificationRules.AsNoTracking().ToListAsync(cancellationToken);

        return entities
            .Select(e => new FileClassificationRuleEntry(e.Id, FileClassificationRuleFactory.Create(
                e.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries),
                FileClassificationFactory.Create(
                    e.Level1,
                    e.Level2 is not null ? Option.Some(e.Level2) : Option.None<string>(),
                    e.Level3 is not null ? Option.Some(e.Level3) : Option.None<string>(),
                    e.IsSpecial))))
            .OrderBy(r => r.Rule.Classification.IsSpecial)
            .ThenBy(r => r.Rule.Classification.Level1)
            .ToList()
            .AsReadOnly();
    }

    public async Task<int> AddAsync(FileClassificationRule rule, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = new FileClassificationRuleEntity
        {
            Keywords = string.Join('|', rule.Keywords),
            Level1 = rule.Classification.Level1,
            Level2 = rule.Classification.Level2.MapOrDefault(v => v, (string?)null),
            Level3 = rule.Classification.Level3.MapOrDefault(v => v, (string?)null),
            IsSpecial = rule.Classification.IsSpecial
        };

        db.FileClassificationRules.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateAsync(int id, FileClassificationRule rule, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.FileClassificationRules.FindAsync([id], cancellationToken);
        if (entity is null)
            return;

        entity.Keywords = string.Join('|', rule.Keywords);
        entity.Level1 = rule.Classification.Level1;
        entity.Level2 = rule.Classification.Level2.MapOrDefault(v => v, (string?)null);
        entity.Level3 = rule.Classification.Level3.MapOrDefault(v => v, (string?)null);
        entity.IsSpecial = rule.Classification.IsSpecial;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.FileClassificationRules.FindAsync([id], cancellationToken);
        if (entity is null)
            return;

        db.FileClassificationRules.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
