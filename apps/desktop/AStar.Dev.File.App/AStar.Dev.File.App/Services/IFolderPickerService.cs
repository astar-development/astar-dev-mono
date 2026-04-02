using System.Threading.Tasks;

namespace AStar.Dev.File.App.Services;

public interface IFolderPickerService
{
    Task<string?> OpenFolderPickerAsync();
}
