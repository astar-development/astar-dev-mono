namespace AStarDev.EFCoreExtensions;

/// <summary>
/// A strongly typed ID backed by a <see cref="string"/>. Fixes <c>TValue</c> so
/// <see cref="StronglyTypedIdExtensions"/> can infer all type arguments.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type.</typeparam>
public interface IStronglyTypedStringId<TSelf> : IStronglyTypedId<TSelf, string>
    where TSelf : IStronglyTypedStringId<TSelf>
{ }
