using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.DependencyInjection;

/// <summary>Creates the service a removed <see cref="ServiceDescriptor"/> describes, so a decorator can wrap a registration it replaced.</summary>
public static class ServiceDescriptorExtensions
{
    /// <summary>Returns the descriptor's instance, the result of its factory, or a new instance of its implementation type built by <see cref="ActivatorUtilities"/>, for keyed and unkeyed descriptors alike.</summary>
    /// <typeparam name="TService">The service the descriptor registers.</typeparam>
    /// <param name="descriptor">The descriptor taken out of the service collection.</param>
    /// <param name="provider">The provider that supplies the factory's or the implementation type's dependencies.</param>
    /// <returns>The service the descriptor describes.</returns>
    public static TService CreateInstance<TService>(this ServiceDescriptor descriptor, IServiceProvider provider)
        where TService : class =>
        (TService)(descriptor.IsKeyedService ? CreateKeyed(descriptor, provider) : CreateUnkeyed(descriptor, provider));

    private static object CreateUnkeyed(ServiceDescriptor descriptor, IServiceProvider provider) =>
        descriptor.ImplementationInstance
        ?? descriptor.ImplementationFactory?.Invoke(provider)
        ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);

    private static object CreateKeyed(ServiceDescriptor descriptor, IServiceProvider provider) =>
        descriptor.KeyedImplementationInstance
        ?? descriptor.KeyedImplementationFactory?.Invoke(provider, descriptor.ServiceKey)
        ?? ActivatorUtilities.CreateInstance(provider, descriptor.KeyedImplementationType!);
}
