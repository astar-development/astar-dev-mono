using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Controls.ApplicationLifetimes;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit;

public class App_should
{
    [Fact]
    public void DoSomething()
    {
        var x = new App();
        x.ApplicationLifetime = new ClassicDesktopStyleApplicationLifetime();
        x.OnFrameworkInitializationCompleted();

       // App.Localisation.ShouldBeOfType<ILocalizationService>();
    }
}
