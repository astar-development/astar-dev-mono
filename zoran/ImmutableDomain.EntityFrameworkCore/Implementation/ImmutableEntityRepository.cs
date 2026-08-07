using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ImmutableDomain.EntityFrameworkCore.Implementation;

[ExcludeFromCodeCoverage]
public class ImmutableEntityRepository<TEntity>(DbContext dbContext, DbSet<TEntity> dbSet, params string[] includes)
    : IImmutableEntityRepository<TEntity>
    where TEntity : class
{
    private string[] Includes { get; } = includes;

    private KeyExpression<TEntity> KeyExpression => field = field ?? new KeyExpression<TEntity>(EntityType);

    private IEntityType EntityType => field = field
            ?? dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).FullName} not found.");

    public async Task<TEntity?> FindImmutableAsync(params object[] keyValues) =>
        await GetDbSetWithIncludes().FirstOrDefaultAsync(KeyExpression.GetEphemeralExpression(keyValues));

    private IQueryable<TEntity> GetDbSetWithIncludes() =>
        Includes.Aggregate((IQueryable<TEntity>)dbSet, (current, include) => current.Include(include));

    public async Task AddImmutableAsync(TEntity entity) =>
        await dbContext.AddAsync(entity);

    public void RemoveImmutable(TEntity entity) =>
        dbContext.Remove(entity);

    public void UpdateImmutable(TEntity entity) =>
        dbContext.UpdateImmutable(entity);
}
