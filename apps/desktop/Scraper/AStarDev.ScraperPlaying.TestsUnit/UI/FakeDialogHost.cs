using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.UI;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

internal sealed class FakeDialogHost : IDialogHost
{
    public Option<string> PickedImportPath { get; set; } = Option.None<string>();

    public Option<string> PickedExportPath { get; set; } = Option.None<string>();

    public Exception? ConfirmFailure { get; set; }

    public bool Confirmed { get; set; }

    public Action OnPick { get; set; } = () => { };

    public int PickCount { get; private set; }

    public int ShownCount { get; private set; }

    public Task<Option<string>> PickImportFileAsync()
    {
        PickCount++;
        OnPick();

        return Task.FromResult(PickedImportPath);
    }

    public Task<Option<string>> PickExportFileAsync()
    {
        PickCount++;
        OnPick();

        return Task.FromResult(PickedExportPath);
    }

    public Task ShowAsync(Window dialog)
    {
        ShownCount++;

        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmLabel)
        => ConfirmFailure is null ? Task.FromResult(Confirmed) : Task.FromException<bool>(ConfirmFailure);
}
