using AStarDev.OneDriveSyncClient.Data;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Startup;
using AStarDev.LoggingOTel.LogViewer;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Startup;

public sealed class GivenShellServiceExtensions
{
    private static readonly EntraIdConfiguration TestEntraIdConfig = new()
    {
        ClientId = "test-client-id",
        RedirectUri = "http://localhost",
        Scopes = ["Files.ReadWrite"],
        AuthorityForMicrosoftAccountsOnly = "https://login.microsoftonline.com/consumers"
    };

    private static ServiceCollection BuildServicesWithShell()
    {
        var services = new ServiceCollection();
        var inMemoryLogProcessor = new InMemoryLogProcessor();

        _ = services.AddLogging();
        _ = services.AddPersistence();
        _ = services.AddLocalizationServices();
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();
        _ = services.AddSingleton(Options.Create(TestEntraIdConfig));
        _ = services.AddShell(inMemoryLogProcessor);

        return services;
    }

    [Fact]
    public void when_add_shell_is_called_then_ifilesystem_is_registered_exactly_once()
    {
        var services = BuildServicesWithShell();

        var fileSystemDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IFileSystem)).ToList();

        fileSystemDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void when_add_shell_is_called_then_the_file_manager_and_file_opener_resolve_with_the_process_launcher()
    {
        using var serviceProvider = BuildServicesWithShell().BuildServiceProvider();

        _ = serviceProvider.GetRequiredService<IProcessLauncher>().ShouldBeOfType<ProcessLauncher>();
        _ = serviceProvider.GetRequiredService<IFileManagerService>().ShouldBeOfType<FileManagerService>();
        _ = serviceProvider.GetRequiredService<IFileOpenerService>().ShouldBeOfType<FileOpenerService>();
    }

    [Fact]
    public void when_all_extension_methods_are_called_then_service_provider_builds_without_throwing()
    {
        var services = BuildServicesWithShell();

        var exception = Record.Exception(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = false }));

        exception.ShouldBeNull();
    }
}
