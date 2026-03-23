using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.Data.Configurations;

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
    public static ComplexPropertyBuilder<TEntity> Configure<TEntity>(this ComplexPropertyBuilder<TEntity> propertyBuilder, IComplexPropertyConfiguration<TEntity> configuration)
    {
        configuration.Configure(propertyBuilder);

        return propertyBuilder;
    }
}
