# AStarDev.DependencyInjection

Helpers over `Microsoft.Extensions.DependencyInjection`.

- `ServiceDescriptor.CreateInstance<TService>(provider)`: returns the instance, factory result or activated type a removed descriptor describes (keyed and unkeyed), so a decorator can wrap a registration it replaced.
