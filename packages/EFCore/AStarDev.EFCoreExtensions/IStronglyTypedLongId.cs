namespace AStarDev.EFCoreExtensions;

/// <summary>
/// A strongly typed ID backed by a <see cref="long"/>. Fixes <c>TValue</c> so
/// <see cref="StronglyTypedIdExtensions"/> can infer all type arguments.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type.</typeparam>
public interface IStronglyTypedLongId<TSelf> : IStronglyTypedId<TSelf, long>
    where TSelf : IStronglyTypedLongId<TSelf>
{ }
