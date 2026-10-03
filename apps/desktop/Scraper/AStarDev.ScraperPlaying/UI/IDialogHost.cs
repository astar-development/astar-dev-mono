using AStarDev.FunctionalParadigm;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The window-bound dialogs the main window's actions need, so the actions' logic does not depend on a live window.</summary>
public interface IDialogHost
{
    /// <summary>Lets the person pick a scrape configuration file to import.</summary>
    /// <returns>The picked path, or <see cref="Option{T}.None"/> when the person cancelled the picker.</returns>
    Task<Option<string>> PickImportFileAsync();

    /// <summary>Lets the person pick a destination file to export the scrape configuration to.</summary>
    /// <returns>The picked path, or <see cref="Option{T}.None"/> when the person cancelled the picker.</returns>
    Task<Option<string>> PickExportFileAsync();

    /// <summary>Shows <paramref name="dialog"/> modally.</summary>
    /// <param name="dialog">The dialog to show.</param>
    Task ShowAsync(Window dialog);

    /// <summary>Asks the person to confirm an action.</summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">What will happen if confirmed.</param>
    /// <param name="confirmLabel">The label of the confirm button.</param>
    /// <returns>Whether the person confirmed.</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel);
}
