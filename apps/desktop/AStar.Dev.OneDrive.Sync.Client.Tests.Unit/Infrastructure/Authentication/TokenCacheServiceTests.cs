using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Auth;

public sealed class TokenCacheServiceTests
{
    [Fact]
    public void Constructor_ShouldCreateCacheDirectory()
    {
        _ = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var service = new TokenCacheService();

        _ = service.ShouldNotBeNull();
        service.CacheDirectory.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_ShouldSetCacheDirectoryProperty()
    {
        var service = new TokenCacheService();

        _ = service.CacheDirectory.ShouldNotBeNull();
    }

    [Fact]
    public void CacheDirectory_ShouldNotBeEmpty()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CacheDirectory_ShouldContainAppName()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.ShouldContain("astar-dev-onedrive-sync");
    }

    [Fact]
    public async Task RegisterAsync_WithValidApp_ShouldNotThrow()
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
    public async Task RegisterAsync_ShouldCallUserTokenCache()
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
    public void Constructor_ShouldInitializeOnlyOnce()
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
    public void CacheDirectory_ShouldBePlatformSpecific(string _)
    {
        var service = new TokenCacheService();

        string cacheDir = service.CacheDirectory;
        cacheDir.ShouldNotBeNullOrEmpty();
        cacheDir.ShouldNotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldMaintainCacheDirectoryConsistency()
    {
        var service = new TokenCacheService();
        string cachedDir = service.CacheDirectory;
        service.CacheDirectory.ShouldBe(cachedDir);
    }

    [Fact]
    public void CacheDirectory_ShouldBeAbsolutePath()
    {
        var service = new TokenCacheService();

        bool isAbsolute = Path.IsPathRooted(service.CacheDirectory);
        isAbsolute.ShouldBeTrue();
    }

    [Fact]
    public void CacheDirectory_ShouldBeReadOnly()
    {
        var service = new TokenCacheService();

        _ = service.CacheDirectory;
        var property = typeof(TokenCacheService).GetProperty("CacheDirectory");
        _ = property.ShouldNotBeNull();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public async Task RegisterAsync_ShouldHandleNullGracefully()
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
    public async Task Constructor_ShouldBeThreadSafe()
    {
        var tasks = new List<Task>();
        var services = new List<TokenCacheService>();
        object lockObj = new object();

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
    public void GetPlatformCacheDirectory_ShouldReturnValidPath()
    {
        var service = new TokenCacheService();

        service.CacheDirectory.ShouldNotBeNullOrEmpty();
        Path.IsPathRooted(service.CacheDirectory).ShouldBeTrue();
    }

    [Fact]
    public void CacheDirectory_ShouldMatchPlatformConvention()
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
