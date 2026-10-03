using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The tag actions the main window's menus trigger.</summary>
public sealed class TagActions(UserOperationRunner operations, TagsBrowser tagsBrowser)
{
    /// <summary>Shows the tags editor.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task EditTagsAsync(IDialogHost dialogs)
        => await operations.ReportFailuresAsync("Unable to edit tags.", async () =>
        {
            if (await tagsBrowser.CreateEditorAsync() is Option<TagsEditorWindow>.Some editor) await dialogs.ShowAsync(editor.Value);
        });
}
