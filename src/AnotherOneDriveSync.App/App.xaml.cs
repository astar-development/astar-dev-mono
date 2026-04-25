using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AnotherOneDriveSync.App.ViewModels;
using AnotherOneDriveSync.Core;
using AnotherOneDriveSync.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnotherOneDriveSync.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AnotherOneDriveSync",
                "sync.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var options = new DbContextOptionsBuilder<SyncDbContext>()
                .UseSqlite($"DataSource={dbPath}")
                .Options;

            var dbContext = new SyncDbContext(options);
            dbContext.Database.Migrate();

            var authService = new AuthService(dbContext, logger);
            var graphClientFactory = new GraphClientFactory(authService);
            var graphService = new GraphService(graphClientFactory, logger);
            var syncService = new SyncService(graphService, dbContext, logger);

            var viewModel = new MainWindowViewModel(authService, graphService, syncService, dbContext, logger);

            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
