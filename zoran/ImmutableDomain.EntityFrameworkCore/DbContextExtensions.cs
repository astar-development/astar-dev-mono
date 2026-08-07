using System.Diagnostics.CodeAnalysis;
using ImmutableDomain.EntityFrameworkCore.Implementation;
using Microsoft.EntityFrameworkCore;

namespace ImmutableDomain.EntityFrameworkCore;

[ExcludeFromCodeCoverage]
public static class DbContextExtensions
{
    public static IImmutableEntityRepository<TEntity> ToImmutableEntityRepository<TEntity>(
        this DbSet<TEntity> dbSet, DbContext dbContext, params string[] includes)
        where TEntity : class =>
        new ImmutableEntityRepository<TEntity>(dbContext, dbSet, includes);
}
