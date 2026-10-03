using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenARootDirectoryCheck : IDisposable
{
    private readonly FakeScrapeConfigurationLookup lookup = new();
    private readonly MockFileSystem fileSystem = new();
    private readonly ServiceProvider serviceProvider;
    private readonly RootDirectoryCheck check;

    public GivenARootDirectoryCheck()
    {
        serviceProvider = new ServiceCollection().AddScoped<IScrapeConfigurationLookup>(_ => lookup).BuildServiceProvider();
        check = new(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), fileSystem);
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_the_root_directory_exists_on_disk_then_it_exists()
    {
        fileSystem.Directory.CreateDirectory("/scrapes/root");
        lookup.RootDirectory = Exceptional.Success(Option.Some("/scrapes/root"));

        var exists = await check.ExistsAsync(TestContext.Current.CancellationToken);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_does_not_exist_on_disk_then_it_does_not_exist()
    {
        lookup.RootDirectory = Exceptional.Success(Option.Some("/scrapes/missing"));

        var exists = await check.ExistsAsync(TestContext.Current.CancellationToken);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_check_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => check.ExistsAsync(cancellation.Token));
    }

    [Fact]
    public async Task when_no_configuration_exists_then_it_throws()
    {
        lookup.RootDirectory = Exceptional.Success(Option.None<string>());

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.ExistsAsync(TestContext.Current.CancellationToken));

        thrown.Message.ShouldBe("Scrape configuration not found");
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_failure_is_rethrown()
    {
        var failure = new InvalidOperationException("query failed");
        lookup.RootDirectory = failure;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.ExistsAsync(TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_original_throw_site_is_kept_in_the_stack_trace()
    {
        lookup.RootDirectory = ThrownFailure.Create("query failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.ExistsAsync(TestContext.Current.CancellationToken));

        ThrownFailure.TraceOf(thrown).ShouldContain(ThrownFailure.ThrowSite);
    }
}
