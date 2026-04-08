using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    public async Task InitialiseAsync(IAuthService authService, IGraphService graphService, IStartupService startupService, ISyncService syncService, SyncScheduler scheduler, ISyncRepository syncRepository,
                                      ISettingsService settingsService, IAccountRepository accountRepository)
    {
        _vm = new MainWindowViewModel(authService, graphService, startupService, syncService, scheduler, syncRepository, settingsService, accountRepository);

        DataContext = _vm;

        await _vm.InitialiseAsync();
    }
}
