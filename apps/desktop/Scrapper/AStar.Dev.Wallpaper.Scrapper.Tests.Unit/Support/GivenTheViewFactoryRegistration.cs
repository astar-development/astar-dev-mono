using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheViewFactoryRegistration
{
    [Fact]
    public void when_a_view_factory_is_registered_then_the_view_itself_is_resolvable()
    {
        var services = new ServiceCollection().AddViewFactory<FakeView>();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<FakeView>().ShouldNotBeNull();
    }

    [Fact]
    public void when_a_view_factory_is_registered_then_the_factory_is_resolvable()
    {
        var services = new ServiceCollection().AddViewFactory<FakeView>();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Func<FakeView>>().ShouldNotBeNull();
    }

    [Fact]
    public void when_the_factory_is_invoked_then_it_creates_a_view_instance()
    {
        var services = new ServiceCollection().AddViewFactory<FakeView>();
        using var provider = services.BuildServiceProvider();

        var view = provider.GetRequiredService<Func<FakeView>>()();

        view.ShouldNotBeNull();
    }

    [Fact]
    public void when_the_factory_is_invoked_twice_then_each_invocation_creates_a_new_instance()
    {
        var services = new ServiceCollection().AddViewFactory<FakeView>();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<FakeView>>();

        var firstView = factory();
        var secondView = factory();

        firstView.ShouldNotBeSameAs(secondView);
    }

    public sealed class FakeView;
}
