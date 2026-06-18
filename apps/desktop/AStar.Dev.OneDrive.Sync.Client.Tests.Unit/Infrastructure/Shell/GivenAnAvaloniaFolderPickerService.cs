using Avalonia.Platform.Storage;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAnAvaloniaFolderPickerService
{
    private const string FolderLocalPath = "/home/user/OneDrive";

    [Fact]
    public async Task when_storage_provider_returns_single_folder_then_local_path_is_returned()
    {
        var folder = Substitute.For<IStorageFolder>();
        folder.Path.Returns(new Uri($"file://{FolderLocalPath}"));
        var storageProvider = Substitute.For<IStorageProvider>();
        storageProvider.OpenFolderPickerAsync(Arg.Any<FolderPickerOpenOptions>())
                       .Returns(Task.FromResult<IReadOnlyList<IStorageFolder>>([folder]));
        var sut = new AvaloniaFolderPickerService();

        string? result = await sut.PickFolderAsync(storageProvider, "Choose a folder", TestContext.Current.CancellationToken);

        result.ShouldBe(FolderLocalPath);
    }

    [Fact]
    public async Task when_storage_provider_returns_empty_list_then_null_is_returned()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        storageProvider.OpenFolderPickerAsync(Arg.Any<FolderPickerOpenOptions>())
                       .Returns(Task.FromResult<IReadOnlyList<IStorageFolder>>([]));
        var sut = new AvaloniaFolderPickerService();

        string? result = await sut.PickFolderAsync(storageProvider, "Choose a folder", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }
}
