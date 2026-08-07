namespace ImmutableDomain.EntityFrameworkCore;

public interface IImmutableEntityRepository<TEntity> where TEntity : class
{
    Task<TEntity?> FindImmutableAsync(params object[] keyValues);
    Task AddImmutableAsync(TEntity entity);
    void RemoveImmutable(TEntity entity);
    void UpdateImmutable(TEntity entity);
}