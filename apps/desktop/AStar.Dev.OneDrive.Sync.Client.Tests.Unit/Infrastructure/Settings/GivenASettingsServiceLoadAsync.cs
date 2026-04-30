using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

public sealed class GivenASettingsServiceLoadAsync
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static void WriteSettingsFile(string filePath, AppSettings settings) =>
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, JsonOpts));

    [Fact]
    public async Task when_settings_file_does_not_exist_then_current_uses_defaults_after_load()
    {
        using var tempDir = new TemporaryDirectory();
        var sut = new SettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_valid_json_file_exists_then_current_is_populated_after_load()
    {
        using var tempDir = new TemporaryDirectory();
        var expected = new AppSettings { Theme = AppTheme.Dark, SyncIntervalMinutes = 15, Locale = "fr-FR" };
        WriteSettingsFile(tempDir.SettingsFilePath, expected);
        var sut = new SettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.Theme.ShouldBe(AppTheme.Dark);
        sut.Current.SyncIntervalMinutes.ShouldBe(15);
        sut.Current.Locale.ShouldBe("fr-FR");
    }

    [Fact]
    public async Task when_json_file_is_malformed_then_current_uses_defaults_after_load()
    {
        using var tempDir = new TemporaryDirectory();
        File.WriteAllText(tempDir.SettingsFilePath, "{ this is not valid json }}}");
        var sut = new SettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_json_file_is_malformed_then_no_exception_is_thrown()
    {
        using var tempDir = new TemporaryDirectory();
        File.WriteAllText(tempDir.SettingsFilePath, "not json at all");
        var sut = new SettingsService(tempDir.SettingsFilePath);

        await Should.NotThrowAsync(() => sut.LoadAsync());
    }

    [Fact]
    public async Task when_json_file_deserializes_to_null_then_current_uses_defaults()
    {
        using var tempDir = new TemporaryDirectory();
        File.WriteAllText(tempDir.SettingsFilePath, "null");
        var sut = new SettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_load_async_is_called_multiple_times_then_current_reflects_latest_file_content()
    {
        using var tempDir = new TemporaryDirectory();
        WriteSettingsFile(tempDir.SettingsFilePath, new AppSettings { Theme = AppTheme.Light });
        var sut = new SettingsService(tempDir.SettingsFilePath);
        await sut.LoadAsync();
        sut.Current.Theme.ShouldBe(AppTheme.Light);

        WriteSettingsFile(tempDir.SettingsFilePath, new AppSettings { Theme = AppTheme.Dark });
        await sut.LoadAsync();

        sut.Current.Theme.ShouldBe(AppTheme.Dark);
    }
}
