namespace AStar.Dev.Source.Generators.Attributes;

/// <summary>
/// Indicates that the target is a strong ID type. Intended for use only on readonly record structs.
/// This is not enforced by the compiler, but should be validated by source generators.
/// </summary>
/// <remarks>
/// Initializes a new instance of the StrongIdAttribute class with the specified identifier type.
/// </remarks>
/// <param name="idType">The type to use as the strong identifier. If null, the default type is Guid.</param>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class StrongIdAttribute(Type? idType = null) : Attribute
{
    /// <summary>
    /// The type of the ID property (e.g., typeof(Guid), typeof(int), typeof(string)). Other types are not supported at the moment.
    /// </summary>
    public Type IdType { get; } = idType ?? typeof(Guid);
}
