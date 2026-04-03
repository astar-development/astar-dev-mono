using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAppSettings : IAsyncLifetime
{
    private readonly AppDbContextFactory _factory = AppDbContextFactory.Create();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task when_theme_mode_is_saved_then_it_can_be_read_back()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var writeCtx = await _factory.CreateContextAsync(ct);
        var settings = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Dark) };
        writeCtx.AppSettings.Add(settings);
        _ = await writeCtx.SaveChangesAsync(ct);

        await using var readCtx = await _factory.CreateContextAsync(ct);
        var loaded = await readCtx.AppSettings.FindAsync([AppSettings.SingletonId], ct);

        loaded.ShouldNotBeNull();
        loaded.ThemeMode.ShouldBe(nameof(ThemeMode.Dark));
    }

    [Fact]
    public async Task when_no_settings_row_exists_then_find_returns_null()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = await _factory.CreateContextAsync(ct);

        var result = await ctx.AppSettings.FindAsync([AppSettings.SingletonId], ct);

        result.ShouldBeNull();
    }
}
