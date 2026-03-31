using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Localisation;

public sealed class GivenLocaleSettings : IAsyncLifetime
{
    private readonly AppDbContextFactory _factory = AppDbContextFactory.Create();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task when_locale_is_saved_then_it_can_be_read_back()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var writeCtx = await _factory.CreateContextAsync(ct);
        var settings = new AppSettings { Id = AppSettings.SingletonId, Locale = "en-GB" };
        writeCtx.AppSettings.Add(settings);
        _ = await writeCtx.SaveChangesAsync(ct);

        await using var readCtx = await _factory.CreateContextAsync(ct);
        var loaded = await readCtx.AppSettings.FindAsync([AppSettings.SingletonId], ct);

        loaded.ShouldNotBeNull();
        loaded.Locale.ShouldBe("en-GB");
    }

    [Fact]
    public async Task when_no_locale_is_set_then_default_is_en_gb()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var writeCtx = await _factory.CreateContextAsync(ct);
        var settings = new AppSettings { Id = AppSettings.SingletonId };
        writeCtx.AppSettings.Add(settings);
        _ = await writeCtx.SaveChangesAsync(ct);

        await using var readCtx = await _factory.CreateContextAsync(ct);
        var loaded = await readCtx.AppSettings.FindAsync([AppSettings.SingletonId], ct);

        loaded.ShouldNotBeNull();
        loaded.Locale.ShouldBe("en-GB");
    }
}
