using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ImmutableDomain.EntityFrameworkCore.Implementation;

[ExcludeFromCodeCoverage]
internal sealed class KeyExpression<TEntity>(IEntityType entityType)
{
    private object[] Fields => field = field ?? new object[Key.Properties.Count];

    private Expression<Func<TEntity, bool>> FixedExpression => field = field ?? GetFixedExpression();

    private IKey Key => field = field
            ?? entityType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey())
            ?? entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).FullName} does not have a primary or alternate key defined.");

    public Expression<Func<TEntity, bool>> GetEphemeralExpression(params object[] values)
    {
        Array.Copy(values, Fields, Fields.Length);
        return FixedExpression;
    }

    private Expression<Func<TEntity, bool>> GetFixedExpression()
    {
        var entityParam = Expression.Parameter(typeof(TEntity), "entity");
        Expression? combinedExpression = null;

        var arrayConstant = Expression.Constant(Fields);
        for (int i = 0; i < Key.Properties.Count; i++)
        {
            var property = Key.Properties[i];
            var propertyAccess = Expression.Property(entityParam, property.PropertyInfo!);
            var arrayElementValue = Expression.ArrayIndex(arrayConstant, Expression.Constant(i));
            var keyValue = Expression.Convert(arrayElementValue, property.ClrType);
            var equality = Expression.Equal(propertyAccess, keyValue);

            combinedExpression = combinedExpression is null
                ? equality
                : Expression.AndAlso(combinedExpression, equality);
        }

        combinedExpression ??= Expression.Constant(true);

        return Expression.Lambda<Func<TEntity, bool>>(combinedExpression, entityParam);
    }
}
