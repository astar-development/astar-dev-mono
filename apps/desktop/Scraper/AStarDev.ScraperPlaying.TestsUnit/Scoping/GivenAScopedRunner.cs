using AStarDev.ScraperPlaying.Scoping;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.Scoping;

public sealed class GivenAScopedRunner : IDisposable
{
    private readonly ServiceProvider serviceProvider = new ServiceCollection().AddScoped<ScopedMarker>().AddScoped<OtherMarker>().AddScoped<ThirdMarker>().BuildServiceProvider();
    private readonly ScopedRunner runner;

    public GivenAScopedRunner() => runner = new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public async Task when_run_then_the_result_of_the_delegate_is_returned()
    {
        var result = await runner.RunAsync<ScopedMarker, int>(_ => Task.FromResult(42));

        result.ShouldBe(42);
    }

    [Fact]
    public async Task when_run_twice_then_each_run_gets_its_own_scope()
    {
        var first = await runner.RunAsync<ScopedMarker, Guid>(marker => Task.FromResult(marker.Id));
        var second = await runner.RunAsync<ScopedMarker, Guid>(marker => Task.FromResult(marker.Id));

        first.ShouldNotBe(second);
    }

    [Fact]
    public async Task when_run_then_the_scope_is_disposed_afterwards()
    {
        var marker = await runner.RunAsync<ScopedMarker, ScopedMarker>(found => Task.FromResult(found));

        marker.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_delegate_throws_then_the_scope_is_still_disposed()
    {
        var captured = new List<ScopedMarker>();

        await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync<ScopedMarker, int>(marker =>
        {
            captured.Add(marker);

            throw new InvalidOperationException("boom");
        }));

        captured.Single().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task when_two_services_are_requested_then_both_come_from_the_same_scope()
    {
        var sameScope = await runner.RunAsync<ScopedMarker, OtherMarker, bool>((marker, other) => Task.FromResult(marker.Id == other.ScopeMarkerId));

        sameScope.ShouldBeTrue();
    }

    [Fact]
    public async Task when_three_services_are_requested_then_all_come_from_the_same_scope()
    {
        var sameScope = await runner.RunAsync<ScopedMarker, OtherMarker, ThirdMarker, bool>((marker, other, third) => Task.FromResult(marker.Id == other.ScopeMarkerId && marker.Id == third.ScopeMarkerId));

        sameScope.ShouldBeTrue();
    }

    [Fact]
    public async Task when_three_services_are_requested_then_the_scope_is_disposed_afterwards()
    {
        var marker = await runner.RunAsync<ScopedMarker, OtherMarker, ThirdMarker, ScopedMarker>((found, _, _) => Task.FromResult(found));

        marker.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_service_is_not_registered_then_the_failure_is_thrown()
        => await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync<UnregisteredMarker, int>(_ => Task.FromResult(1)));

    public void Dispose() => serviceProvider.Dispose();

    private sealed class ScopedMarker : IDisposable
    {
        public Guid Id { get; } = Guid.CreateVersion7();

        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class OtherMarker(ScopedMarker marker)
    {
        public Guid ScopeMarkerId { get; } = marker.Id;
    }

    private sealed class ThirdMarker(ScopedMarker marker)
    {
        public Guid ScopeMarkerId { get; } = marker.Id;
    }

    private sealed class UnregisteredMarker;
}
