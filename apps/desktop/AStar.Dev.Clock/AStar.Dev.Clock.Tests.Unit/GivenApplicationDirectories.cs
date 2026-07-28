using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Clock.Tests.Unit;

public sealed class GivenApplicationDirectories
{
    private static ApplicationDirectories CreateSut(MockFileSystem fileSystem) => new(fileSystem, Substitute.For<ILogger<ApplicationDirectories>>());

    [Fact]
    public void when_create_if_required_is_called_then_the_data_directory_is_created()
    {
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        sut.CreateIfRequired();

        fileSystem.Directory.Exists(ApplicationDirectories.DataDirectory).ShouldBeTrue();
    }

    [Fact]
    public void when_create_if_required_is_called_then_the_cache_directory_is_created()
    {
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        sut.CreateIfRequired();

        fileSystem.Directory.Exists(ApplicationDirectories.CacheDirectory).ShouldBeTrue();
    }

    [Fact]
    public void when_create_if_required_is_called_then_the_logs_directory_is_created()
    {
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        sut.CreateIfRequired();

        fileSystem.Directory.Exists(ApplicationDirectories.LogsDirectory).ShouldBeTrue();
    }

    [Fact]
    public void when_data_directory_is_read_then_it_ends_with_data() =>
        ApplicationDirectories.DataDirectory.ShouldEndWith("data");

    [Fact]
    public void when_data_directory_is_read_then_it_contains_the_application_folder() =>
        ApplicationDirectories.DataDirectory.ShouldContain(ApplicationMetadata.ApplicationFolder);

    [Fact]
    public void when_cache_directory_is_read_then_it_ends_with_cache() =>
        ApplicationDirectories.CacheDirectory.ShouldEndWith("cache");

    [Fact]
    public void when_cache_directory_is_read_then_it_contains_the_application_folder() =>
        ApplicationDirectories.CacheDirectory.ShouldContain(ApplicationMetadata.ApplicationFolder);

    [Fact]
    public void when_logs_directory_is_read_then_it_ends_with_logs() =>
        ApplicationDirectories.LogsDirectory.ShouldEndWith("logs");

    [Fact]
    public void when_logs_directory_is_read_then_it_contains_the_application_folder() =>
        ApplicationDirectories.LogsDirectory.ShouldContain(ApplicationMetadata.ApplicationFolder);
}
