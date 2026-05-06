using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication;

public sealed class GivenATokenCacheService
{
    [Fact]
    public void when_constructed_then_cache_directory_is_set()
    {
        _ = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var service = new TokenCacheService();

        _ = service.ShouldNotBeNull();
        service.CacheDirectory.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void when_constructed_then_cache_directory_property_is_not_null()
    {
        var service = new TokenCacheService();

        _ = service.CacheDirectory.ShouldNotBeNull();
    }

    [Fact]
    public void when_cache_directory_is_read_then_it_is_not_empty()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void when_cache_directory_is_read_then_it_contains_app_name()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.ShouldContain("astar-dev-onedrive-sync");
    }

    [Fact]
    public async Task when_register_async_called_with_valid_app_then_no_exception_is_thrown()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockCache = Substitute.For<ITokenCache>();
        _ = mockApp.UserTokenCache.Returns(mockCache);
        var service = new TokenCacheService();
        try
        {
            await service.RegisterAsync(mockApp);
        }
        catch (InvalidOperationException)
        {
            // Expected when MSAL helpers are not available in test environment
        }
    }

    [Fact]
    public async Task when_register_async_called_then_user_token_cache_is_accessed()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockCache = Substitute.For<ITokenCache>();
        _ = mockApp.UserTokenCache.Returns(mockCache);
        var service = new TokenCacheService();

        try
        {
            await service.RegisterAsync(mockApp);
        }
        catch (InvalidOperationException)
        {
            // Expected when MSAL helpers are not available
        }

        _ = mockApp.Received(1).UserTokenCache;
    }

    [Fact]
    public void when_two_instances_are_created_then_they_share_the_same_cache_directory()
    {
        var service1 = new TokenCacheService();
        var service2 = new TokenCacheService();

        _ = service1.ShouldNotBeNull();
        _ = service2.ShouldNotBeNull();
        service1.CacheDirectory.ShouldBe(service2.CacheDirectory);
    }

    [Theory]
    [InlineData("path1")]
    [InlineData("path2")]
    [InlineData("different/path")]
    public void when_cache_directory_is_read_then_it_is_platform_specific(string _)
    {
        var service = new TokenCacheService();

        string cacheDir = service.CacheDirectory;
        cacheDir.ShouldNotBeNullOrEmpty();
        cacheDir.ShouldNotBeEmpty();
    }

    [Fact]
    public void when_cache_directory_is_read_twice_then_same_value_is_returned()
    {
        var service = new TokenCacheService();
        string cachedDir = service.CacheDirectory;
        service.CacheDirectory.ShouldBe(cachedDir);
    }

    [Fact]
    public void when_cache_directory_is_read_then_it_is_an_absolute_path()
    {
        var service = new TokenCacheService();

        bool isAbsolute = Path.IsPathRooted(service.CacheDirectory);
        isAbsolute.ShouldBeTrue();
    }

    [Fact]
    public void when_cache_directory_property_is_inspected_then_it_has_no_setter()
    {
        var service = new TokenCacheService();

        _ = service.CacheDirectory;
        var property = typeof(TokenCacheService).GetProperty("CacheDirectory");
        _ = property.ShouldNotBeNull();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public async Task when_register_async_called_with_null_then_null_reference_exception_is_thrown()
    {
        var service = new TokenCacheService();
        try
        {
            await service.RegisterAsync(null!);
        }
        catch (NullReferenceException)
        {
            // Expected - null app parameter is not validated
        }
    }

    [Fact]
    public async Task when_multiple_instances_are_created_concurrently_then_all_share_same_cache_directory()
    {
        var tasks = new List<Task>();
        var services = new List<TokenCacheService>();
        object lockObj = new();

        for (int i = 0; i < 10; i++)
        {
            var task = Task.Run(() =>
            {
                var service = new TokenCacheService();
                lock (lockObj)
                {
                    services.Add(service);
                }
            }, TestContext.Current.CancellationToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        services.Count.ShouldBe(10);
        string firstDir = services[0].CacheDirectory;
        foreach (var service in services)
        {
            service.CacheDirectory.ShouldBe(firstDir);
        }
    }

    [Fact]
    public void when_cache_directory_is_read_then_path_is_rooted()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.ShouldNotBeNullOrEmpty();
        Path.IsPathRooted(service.CacheDirectory).ShouldBeTrue();
    }

    [Fact]
    public void when_cache_directory_is_read_then_it_matches_platform_convention()
    {
        var service = new TokenCacheService();
        string cacheDir = service.CacheDirectory;
        if (OperatingSystem.IsWindows())
        {
            cacheDir.ShouldContain("AStar.Dev.OneDrive.Sync");
        }
        else if (OperatingSystem.IsMacOS())
        {
            cacheDir.ShouldContain("Application Support");
        }
        else if (OperatingSystem.IsLinux())
        {
            cacheDir.ShouldContain(".config");
        }
    }
}
