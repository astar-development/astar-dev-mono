using System.IO.Abstractions;
using AStar.Dev.File.App.Services;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.File.App.TestsUnit.Services;

public class GivenApplicationDirectories
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ILogger<ApplicationDirectories> _logger = Substitute.For<ILogger<ApplicationDirectories>>();
    private readonly ApplicationDirectories _sut;

    public GivenApplicationDirectories()
    {
        _fileSystem.Directory.Returns(Substitute.For<IDirectory>());
        _sut = new ApplicationDirectories(_fileSystem, _logger);
    }

    [Fact]
    public void when_create_if_required_is_called_then_the_data_directory_is_created()
    {
        _sut.CreateIfRequired();

        _fileSystem.Directory.Received(1).CreateDirectory(ApplicationDirectories.DataDirectory);
    }

    [Fact]
    public void when_create_if_required_is_called_then_the_logs_directory_is_created()
    {
        _sut.CreateIfRequired();

        _fileSystem.Directory.Received(1).CreateDirectory(ApplicationDirectories.LogsDirectory);
    }

    [Fact]
    public void when_create_if_required_is_called_then_the_cache_directory_is_created()
    {
        _sut.CreateIfRequired();

        _fileSystem.Directory.Received(1).CreateDirectory(ApplicationDirectories.CacheDirectory);
    }

    [Fact]
    public void when_data_directory_is_read_then_it_ends_with_the_data_segment()
        => ApplicationDirectories.DataDirectory.ShouldEndWith(Path.Combine(ApplicationMetadata.ApplicationFolder, "data"));

    [Fact]
    public void when_cache_directory_is_read_then_it_ends_with_the_cache_segment()
        => ApplicationDirectories.CacheDirectory.ShouldEndWith(Path.Combine(ApplicationMetadata.ApplicationFolder, "cache"));

    [Fact]
    public void when_logs_directory_is_read_then_it_ends_with_the_logs_segment()
        => ApplicationDirectories.LogsDirectory.ShouldEndWith(Path.Combine(ApplicationMetadata.ApplicationFolder, "logs"));
}
