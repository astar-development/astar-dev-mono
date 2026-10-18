namespace AStarDev.EFCoreExtensions;

/// <summary>
/// A strongly typed ID backed by a <see cref="Guid"/>. Fixes <c>TValue</c> so
/// <see cref="StronglyTypedIdExtensions"/> can infer all type arguments.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type.</typeparam>
public interface IStronglyTypedGuidId<TSelf> : IStronglyTypedId<TSelf, Guid>
    where TSelf : IStronglyTypedGuidId<TSelf>
{ }
