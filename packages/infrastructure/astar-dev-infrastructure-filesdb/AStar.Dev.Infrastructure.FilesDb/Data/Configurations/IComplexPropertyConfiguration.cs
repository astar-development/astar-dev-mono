using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

/// <summary>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IComplexPropertyConfiguration<TEntity>
{
    /// <summary>
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    void Configure(ComplexPropertyBuilder<TEntity> builder);
}