using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow() => InitializeComponent();

    public async Task InitialiseAsync(IAuthService authService, IGraphService graphService, IStartupService startupService, ISyncService syncService, SyncScheduler scheduler, ISyncRepository syncRepository,
                                      ISettingsService settingsService, IAccountRepository accountRepository)
    {
        _vm = new MainWindowViewModel(authService, graphService, startupService, syncService, scheduler, syncRepository, settingsService, accountRepository);

        DataContext = _vm;

        await _vm.InitialiseAsync();
    }
}
