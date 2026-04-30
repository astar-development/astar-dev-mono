using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

public sealed class GivenASettingsServiceLoadAsync
{
    private const string SettingsPath = "/app/settings.json";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static MockFileSystem CreateMockFsWithSettings(AppSettings settings)
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory("/app");
        mockFs.AddFile(SettingsPath, new MockFileData(JsonSerializer.Serialize(settings, JsonOpts)));

        return mockFs;
    }

    private static MockFileSystem CreateEmptyMockFs()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory("/app");

        return mockFs;
    }

    [Fact]
    public async Task when_settings_file_does_not_exist_then_current_uses_defaults_after_load()
    {
        var sut = new SettingsService(CreateEmptyMockFs(), SettingsPath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_valid_json_file_exists_then_current_is_populated_after_load()
    {
        var expected = new AppSettings { Theme = AppTheme.Dark, SyncIntervalMinutes = 15, Locale = "fr-FR" };
        var sut = new SettingsService(CreateMockFsWithSettings(expected), SettingsPath);

        await sut.LoadAsync();

        sut.Current.Theme.ShouldBe(AppTheme.Dark);
        sut.Current.SyncIntervalMinutes.ShouldBe(15);
        sut.Current.Locale.ShouldBe("fr-FR");
    }

    [Fact]
    public async Task when_json_file_is_malformed_then_current_uses_defaults_after_load()
    {
        var mockFs = CreateEmptyMockFs();
        mockFs.AddFile(SettingsPath, new MockFileData("{ this is not valid json }}}"));
        var sut = new SettingsService(mockFs, SettingsPath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_json_file_is_malformed_then_no_exception_is_thrown()
    {
        var mockFs = CreateEmptyMockFs();
        mockFs.AddFile(SettingsPath, new MockFileData("not json at all"));
        var sut = new SettingsService(mockFs, SettingsPath);

        await Should.NotThrowAsync(() => sut.LoadAsync());
    }

    [Fact]
    public async Task when_json_file_deserializes_to_null_then_current_uses_defaults()
    {
        var mockFs = CreateEmptyMockFs();
        mockFs.AddFile(SettingsPath, new MockFileData("null"));
        var sut = new SettingsService(mockFs, SettingsPath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_load_async_is_called_multiple_times_then_current_reflects_latest_file_content()
    {
        var mockFs = CreateMockFsWithSettings(new AppSettings { Theme = AppTheme.Light });
        var sut = new SettingsService(mockFs, SettingsPath);
        await sut.LoadAsync();
        sut.Current.Theme.ShouldBe(AppTheme.Light);

        mockFs.File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new AppSettings { Theme = AppTheme.Dark }, JsonOpts));
        await sut.LoadAsync();

        sut.Current.Theme.ShouldBe(AppTheme.Dark);
    }
}
