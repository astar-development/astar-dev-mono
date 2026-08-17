using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public static class ComplexPropertyBuilderConfiguration
{
    /// <summary>
    /// </summary>
    /// <param name="propertyBuilder"></param>
    /// <param name="configuration"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static ComplexPropertyBuilder<TEntity> Configure<TEntity>(this ComplexPropertyBuilder<TEntity> propertyBuilder, IComplexPropertyConfiguration<TEntity> configuration) where TEntity : notnull
    {
        configuration.Configure(propertyBuilder);

        return propertyBuilder;
    }
}
