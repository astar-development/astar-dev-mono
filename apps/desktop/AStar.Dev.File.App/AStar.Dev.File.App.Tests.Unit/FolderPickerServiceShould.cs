using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class FolderPickerServiceShould
{
    private readonly FolderPickerService _sut = new();

    [Fact]
    public async Task OpenFolderPickerAsync_WithNoApplicationContext_ReturnsNull()
    {
        string? result = await _sut.OpenFolderPickerAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public void FolderPickerService_CanBeInstantiated()
    {
        var service = new FolderPickerService();
        service.ShouldNotBeNull();
    }
}
