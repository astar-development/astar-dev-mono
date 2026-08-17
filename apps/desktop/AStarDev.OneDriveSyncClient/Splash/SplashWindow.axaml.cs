using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using Avalonia.Controls;
using Microsoft.Extensions.Options;

namespace AStarDev.OneDriveSyncClient.Splash;

[ExcludeFromCodeCoverage]
public partial class SplashWindow : Window
{
    private readonly SplashWindowViewModel viewModel;

    public SplashWindow() : this(Options.Create(new ClientConfiguration { ApplicationName = string.Empty })) { }

    public SplashWindow(IOptions<ClientConfiguration> config)
    {
        viewModel = new SplashWindowViewModel { AppName = config.Value.ApplicationName };
        InitializeComponent();
        DataContext = viewModel;
    }

    public void SetStatus(string status) => viewModel.Status = status;
}
