using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Packages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AStarDev.Web.Tests.Unit.Packages;

public class GivenANugetPackageService : IDisposable
{
    private readonly INugetApiClient apiClient = Substitute.For<INugetApiClient>();
    private readonly MemoryCache cache = new(new MemoryCacheOptions());
    private readonly ILogger<NugetPackageService> logger = Substitute.For<ILogger<NugetPackageService>>();

    private NugetPackageService CreateSut() => new(apiClient, cache, logger);

    public void Dispose()
    {
        cache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task when_the_live_fetch_succeeds_then_the_fetched_package_is_returned()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Utilities", 1000, "https://www.nuget.org/packages/AStarDev.Utilities");
        apiClient.FetchAsync("AStarDev.Utilities", TestContext.Current.CancellationToken).Returns(Option.Some(package));
        var sut = CreateSut();

        var result = await sut.GetPackageDataAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);

        result.Match(ok => ok, err => throw new InvalidOperationException(err)).ShouldBe(package);
    }

    [Fact]
    public async Task when_a_fresh_value_is_already_cached_then_the_api_client_is_not_called_again()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Utilities", 1000, "https://www.nuget.org/packages/AStarDev.Utilities");
        apiClient.FetchAsync("AStarDev.Utilities", TestContext.Current.CancellationToken).Returns(Option.Some(package));
        var sut = CreateSut();
        await sut.GetPackageDataAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);

        await sut.GetPackageDataAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);

        await apiClient.Received(1).FetchAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_the_live_fetch_fails_but_a_last_known_good_value_exists_then_the_stale_value_is_returned()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Utilities", 1000, "https://www.nuget.org/packages/AStarDev.Utilities");
        apiClient.FetchAsync("AStarDev.Utilities", TestContext.Current.CancellationToken).Returns(Option.Some(package), Option.None<PackageData>());
        var sut = CreateSut();
        await sut.GetPackageDataAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);
        cache.Remove("nuget:fresh:AStarDev.Utilities");

        var result = await sut.GetPackageDataAsync("AStarDev.Utilities", TestContext.Current.CancellationToken);

        result.Match(ok => ok, err => throw new InvalidOperationException(err)).ShouldBe(package);
    }

    [Fact]
    public async Task when_the_live_fetch_fails_and_no_cached_value_exists_then_an_error_is_returned()
    {
        apiClient.FetchAsync("AStar.Dev.Unknown", TestContext.Current.CancellationToken).Returns(Option.None<PackageData>());
        var sut = CreateSut();

        var result = await sut.GetPackageDataAsync("AStar.Dev.Unknown", TestContext.Current.CancellationToken);

        result.Match(ok => (PackageData?)ok, err => null).ShouldBeNull();
    }
}
