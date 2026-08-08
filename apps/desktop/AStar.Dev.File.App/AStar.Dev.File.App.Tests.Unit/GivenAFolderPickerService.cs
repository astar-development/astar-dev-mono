using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class GivenAFolderPickerService
{
    private readonly FolderPickerService _sut = new();

    [Fact]
    public async Task when_opening_folder_picker_without_application_context_then_returns_null()
    {
        string? result = await _sut.OpenFolderPickerAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var service = new FolderPickerService();
        service.ShouldNotBeNull();
    }
}
