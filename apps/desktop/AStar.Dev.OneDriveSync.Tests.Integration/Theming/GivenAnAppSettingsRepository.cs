using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Theming;

public sealed class GivenAnAppSettingsRepository : IAsyncLifetime
{
    private readonly AppDbContextFactory _dbFactory = AppDbContextFactory.Create();
    private AppSettingsRepository _sut = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbFactory.CreateContextAsync();
        var dbContextFactory = new TestDbContextFactory(_dbFactory);
        _sut = new AppSettingsRepository(dbContextFactory, NullLogger<AppSettingsRepository>.Instance);
    }

    public async ValueTask DisposeAsync() => await _dbFactory.DisposeAsync();

    [Fact]
    public async Task when_get_is_called_and_no_row_exists_then_returns_ok_with_null()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await _sut.GetAsync(ct);

        result.ShouldBeOfType<Result<AppSettings?, ErrorResponse>.Ok>();
        ((Result<AppSettings?, ErrorResponse>.Ok)result).Value.ShouldBeNull();
    }

    [Fact]
    public async Task when_save_is_called_then_returns_ok_with_the_saved_settings()
    {
        var ct = TestContext.Current.CancellationToken;
        var settings = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Dark) };

        var result = await _sut.SaveAsync(settings, ct);

        result.ShouldBeOfType<Result<AppSettings, ErrorResponse>.Ok>();
        ((Result<AppSettings, ErrorResponse>.Ok)result).Value.ThemeMode.ShouldBe(nameof(ThemeMode.Dark));
    }

    [Fact]
    public async Task when_save_is_called_and_no_row_exists_then_a_row_is_inserted()
    {
        var ct = TestContext.Current.CancellationToken;
        var settings = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Light) };
        _ = await _sut.SaveAsync(settings, ct);

        var result = await _sut.GetAsync(ct);

        var loaded = ((Result<AppSettings?, ErrorResponse>.Ok)result).Value;
        loaded.ShouldNotBeNull();
        loaded.ThemeMode.ShouldBe(nameof(ThemeMode.Light));
    }

    [Fact]
    public async Task when_save_is_called_twice_then_the_second_call_updates_the_existing_row()
    {
        var ct = TestContext.Current.CancellationToken;
        _ = await _sut.SaveAsync(new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Light) }, ct);

        _ = await _sut.SaveAsync(new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Dark) }, ct);

        var result = await _sut.GetAsync(ct);
        var loaded = ((Result<AppSettings?, ErrorResponse>.Ok)result).Value;
        loaded.ShouldNotBeNull();
        loaded.ThemeMode.ShouldBe(nameof(ThemeMode.Dark));
    }

    [Fact]
    public async Task when_get_is_called_and_a_row_exists_then_returns_ok_with_the_settings()
    {
        var ct = TestContext.Current.CancellationToken;
        _ = await _sut.SaveAsync(new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Auto) }, ct);

        var result = await _sut.GetAsync(ct);

        var loaded = ((Result<AppSettings?, ErrorResponse>.Ok)result).Value;
        loaded.ShouldNotBeNull();
        loaded.ThemeMode.ShouldBe(nameof(ThemeMode.Auto));
    }
}
