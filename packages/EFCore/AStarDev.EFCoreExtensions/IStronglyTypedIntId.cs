namespace AStarDev.EFCoreExtensions;

/// <summary>
/// A strongly typed ID backed by a <see cref="int"/>. Fixes <c>TValue</c> so
/// <see cref="StronglyTypedIdExtensions"/> can infer all type arguments.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type.</typeparam>
public interface IStronglyTypedIntId<TSelf> : IStronglyTypedId<TSelf, int>
    where TSelf : IStronglyTypedIntId<TSelf>
{ }
