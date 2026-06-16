using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Startup;

public sealed class GivenShellServiceExtensions
{
    private static readonly EntraIdConfiguration TestEntraIdConfig = new
    (
        "test-client-id",
        "http://localhost",
        ["Files.ReadWrite"],
        "https://login.microsoftonline.com/consumers"
    );

    private static ServiceCollection BuildServicesWithShell()
    {
        var services = new ServiceCollection();
        var inMemoryLogSink = new InMemoryLogSink();

        _ = services.AddLogging();
        _ = services.AddPersistence();
        _ = services.AddLocalizationServices();
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();
        _ = services.AddSingleton(Options.Create(TestEntraIdConfig));
        _ = services.AddShell(inMemoryLogSink);

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
    public void when_all_extension_methods_are_called_then_service_provider_builds_without_throwing()
    {
        var services = BuildServicesWithShell();

        var exception = Record.Exception(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = false }));

        exception.ShouldBeNull();
    }
}
