using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.DependencyInjection.TestsUnit;

public sealed class GivenARemovedServiceDescriptor : IDisposable
{
    private const string Key = "undecorated";

    private readonly FakeTimeProvider clock = new();
    private readonly ServiceProvider provider;

    public GivenARemovedServiceDescriptor() => provider = new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider();

    public void Dispose() => provider.Dispose();

    [Fact]
    public void when_the_descriptor_holds_an_instance_then_that_instance_is_returned()
    {
        var instance = new DescribedService(clock);
        var descriptor = ServiceDescriptor.Singleton<IDescribedService>(instance);

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        created.ShouldBeSameAs(instance);
    }

    [Fact]
    public void when_the_descriptor_holds_a_factory_then_the_factory_runs_with_the_provider()
    {
        IServiceProvider? seenProvider = null;
        var descriptor = ServiceDescriptor.Singleton<IDescribedService>(serviceProvider =>
        {
            seenProvider = serviceProvider;

            return new DescribedService(serviceProvider.GetRequiredService<TimeProvider>());
        });

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        seenProvider.ShouldBeSameAs(provider);
        created.Clock.ShouldBeSameAs(clock);
    }

    [Fact]
    public void when_the_descriptor_holds_a_type_then_a_new_instance_is_built_from_the_provider()
    {
        var descriptor = ServiceDescriptor.Singleton<IDescribedService, DescribedService>();

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        created.ShouldBeOfType<DescribedService>().Clock.ShouldBeSameAs(clock);
    }

    [Fact]
    public void when_the_descriptor_holds_a_type_then_each_call_builds_a_new_instance()
    {
        var descriptor = ServiceDescriptor.Singleton<IDescribedService, DescribedService>();

        var first = descriptor.CreateInstance<IDescribedService>(provider);
        var second = descriptor.CreateInstance<IDescribedService>(provider);

        second.ShouldNotBeSameAs(first);
    }

    [Fact]
    public void when_the_keyed_descriptor_holds_an_instance_then_that_instance_is_returned()
    {
        var instance = new DescribedService(clock);
        var descriptor = ServiceDescriptor.KeyedSingleton<IDescribedService>(Key, instance);

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        created.ShouldBeSameAs(instance);
    }

    [Fact]
    public void when_the_keyed_descriptor_holds_a_factory_then_the_factory_runs_with_the_provider_and_the_key()
    {
        object? seenKey = null;
        var descriptor = ServiceDescriptor.KeyedSingleton<IDescribedService>(Key, (serviceProvider, serviceKey) =>
        {
            seenKey = serviceKey;

            return new DescribedService(serviceProvider.GetRequiredService<TimeProvider>());
        });

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        seenKey.ShouldBe(Key);
        created.Clock.ShouldBeSameAs(clock);
    }

    [Fact]
    public void when_the_keyed_descriptor_holds_a_type_then_a_new_instance_is_built_from_the_provider()
    {
        var descriptor = ServiceDescriptor.KeyedSingleton<IDescribedService, DescribedService>(Key);

        var created = descriptor.CreateInstance<IDescribedService>(provider);

        created.ShouldBeOfType<DescribedService>().Clock.ShouldBeSameAs(clock);
    }
}
