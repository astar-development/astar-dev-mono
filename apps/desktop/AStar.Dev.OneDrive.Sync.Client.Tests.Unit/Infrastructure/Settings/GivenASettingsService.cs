using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

[Collection("SettingsService")]
public sealed class GivenASettingsService : IDisposable
{
    private readonly TemporaryDirectory tempDir = new();

    public void Dispose() => tempDir.Dispose();

    [Fact]
    public void when_constructed_then_service_implements_isettings_service() =>
        new SettingsService(tempDir.SettingsFilePath).ShouldBeAssignableTo<ISettingsService>();

    [Fact]
    public async Task when_load_async_is_called_then_service_still_implements_isettings_service()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        await service.LoadAsync();

        service.ShouldBeAssignableTo<ISettingsService>();
    }

    [Fact]
    public async Task when_save_async_is_called_without_subscribers_then_no_exception_is_thrown()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        await Should.NotThrowAsync(() => service.SaveAsync());
    }

    [Fact]
    public async Task when_save_async_is_called_multiple_times_then_settings_changed_is_raised_each_time()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);
        var raiseCount = 0;
        service.SettingsChanged += (_, _) => raiseCount++;

        await service.SaveAsync();
        await service.SaveAsync();

        raiseCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_settings_changed_event_is_raised_then_sender_is_the_service_instance()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);
        object? capturedSender = null;
        service.SettingsChanged += (sender, _) => capturedSender = sender;

        await service.SaveAsync();

        capturedSender.ShouldBeSameAs(service);
    }
}
