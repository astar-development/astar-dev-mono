using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

[Collection("SettingsService")]
public sealed class GivenASettingsService
{
    private const string SettingsPath = "/app/settings.json";

    private static MockFileSystem CreateMockFileSystem()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/app");

        return mockFileSystem;
    }

    private static SettingsService CreateService(string? path = SettingsPath)
        => new(CreateMockFileSystem(), Substitute.For<ILogger<SettingsService>>(), path);

    [Fact]
    public void when_constructed_then_service_implements_isettings_service() =>
        CreateService().ShouldBeAssignableTo<ISettingsService>();

    [Fact]
    public async Task when_load_async_is_called_then_service_still_implements_isettings_service()
    {
        var service = CreateService();

        await service.LoadAsync();

        service.ShouldBeAssignableTo<ISettingsService>();
    }

    [Fact]
    public async Task when_save_async_is_called_without_subscribers_then_no_exception_is_thrown()
    {
        var service = CreateService();

        await Should.NotThrowAsync(() => service.SaveAsync());
    }

    [Fact]
    public async Task when_save_async_is_called_multiple_times_then_settings_changed_is_raised_each_time()
    {
        var service = CreateService();
        int raiseCount = 0;
        service.SettingsChanged += (_, _) => raiseCount++;

        await service.SaveAsync();
        await service.SaveAsync();

        raiseCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_settings_changed_event_is_raised_then_sender_is_the_service_instance()
    {
        var service = CreateService();
        object? capturedSender = null;
        service.SettingsChanged += (sender, _) => capturedSender = sender;

        await service.SaveAsync();

        capturedSender.ShouldBeSameAs(service);
    }

    [Fact]
    public async Task when_hacker_serialized_then_round_trips_correctly()
    {
        var fileSystem = CreateMockFileSystem();
        var writeService = new SettingsService(fileSystem, Substitute.For<ILogger<SettingsService>>(), SettingsPath);
        writeService.Current.Theme = AppTheme.Hacker;

        await writeService.SaveAsync();

        var readService = new SettingsService(fileSystem, Substitute.For<ILogger<SettingsService>>(), SettingsPath);
        await readService.LoadAsync();

        readService.Current.Theme.ShouldBe(AppTheme.Hacker);
    }
}
