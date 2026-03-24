using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.Data.Configurations;

/// <summary>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IComplexPropertyConfiguration<TEntity> where TEntity : class // Should be more specific
{
    /// <summary>
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    void Configure(ComplexPropertyBuilder<TEntity> builder);
}
