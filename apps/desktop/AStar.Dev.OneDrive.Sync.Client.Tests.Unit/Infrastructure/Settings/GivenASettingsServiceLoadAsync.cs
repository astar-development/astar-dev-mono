using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

public sealed class GivenASettingsServiceLoadAsync
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static void WriteSettingsFile(string directory, AppSettings settings)
    {
        var filePath = System.IO.Path.Combine(directory, "settings.json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, JsonOpts));
    }

    [Fact]
    public async Task when_settings_file_does_not_exist_then_current_uses_defaults_after_load()
    {
        var sut = new SettingsService();

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_valid_json_file_exists_then_current_is_populated_after_load()
    {
        using var tempDir = new TemporaryDirectory();
        var expected = new AppSettings { Theme = AppTheme.Dark, SyncIntervalMinutes = 15, Locale = "fr-FR" };
        WriteSettingsFile(tempDir.Path, expected);

        var sut = new TestableSettingsService(tempDir.SettingsFilePath);

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

        var sut = new TestableSettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
        sut.Current.Theme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public async Task when_json_file_is_malformed_then_no_exception_is_thrown()
    {
        using var tempDir = new TemporaryDirectory();
        File.WriteAllText(tempDir.SettingsFilePath, "not json at all");

        var sut = new TestableSettingsService(tempDir.SettingsFilePath);

        await Should.NotThrowAsync(() => sut.LoadAsync());
    }

    [Fact]
    public async Task when_json_file_deserializes_to_null_then_current_uses_defaults()
    {
        using var tempDir = new TemporaryDirectory();
        File.WriteAllText(tempDir.SettingsFilePath, "null");

        var sut = new TestableSettingsService(tempDir.SettingsFilePath);

        await sut.LoadAsync();

        sut.Current.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_load_async_is_called_multiple_times_then_current_reflects_latest_file_content()
    {
        using var tempDir = new TemporaryDirectory();
        WriteSettingsFile(tempDir.Path, new AppSettings { Theme = AppTheme.Light });

        var sut = new TestableSettingsService(tempDir.SettingsFilePath);
        await sut.LoadAsync();
        sut.Current.Theme.ShouldBe(AppTheme.Light);

        WriteSettingsFile(tempDir.Path, new AppSettings { Theme = AppTheme.Dark });
        await sut.LoadAsync();

        sut.Current.Theme.ShouldBe(AppTheme.Dark);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("settings-test-").FullName;
        public string SettingsFilePath => System.IO.Path.Combine(Path, "settings.json");

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class TestableSettingsService(string filePath) : ISettingsService
    {
        public AppSettings Current { get; private set; } = new();

        public event EventHandler<AppSettings>? SettingsChanged;

        public async Task LoadAsync()
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                await using var stream = File.OpenRead(filePath);
                Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOpts).ConfigureAwait(false) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "[TestableSettingsService] Failed to deserialize settings from {Path}; using defaults", filePath);
                Current = new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, Current, JsonOpts).ConfigureAwait(false);
            SettingsChanged?.Invoke(this, Current);
        }
    }
}
