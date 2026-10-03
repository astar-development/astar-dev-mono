using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The scrape configuration actions the main window's menus trigger: import, export and edit.</summary>
public sealed class ConfigurationActions(
    UserOperationRunner operations,
    StatusReporter status,
    ConfigurationTransfer transfer,
    ConfigurationBrowser configurationBrowser)
{
    /// <summary>Imports a scrape configuration from a file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task ImportConfigurationAsync(IDialogHost dialogs)
        => await operations.RunAsync("Scrape configuration import cancelled.", "Unable to import scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForImport(await transfer.ImportPickedFileAsync(dialogs, cancellationToken))));

    /// <summary>Exports the scrape configuration to a file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    /// <param name="apiKeys">Whether the exported file includes the API keys.</param>
    public async Task ExportConfigurationAsync(IDialogHost dialogs, ApiKeyExport apiKeys)
        => await operations.RunAsync("Scrape configuration export cancelled.", "Unable to export scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForExport(await transfer.ExportPickedFileAsync(dialogs, apiKeys, cancellationToken), apiKeys)));

    /// <summary>Shows the scrape configuration editor.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task EditConfigurationAsync(IDialogHost dialogs)
        => await operations.ReportFailuresAsync("Unable to edit scrape configuration.", async () =>
        {
            if (await configurationBrowser.CreateEditorAsync() is Option<ConfigurationEditorWindow>.Some editor) await dialogs.ShowAsync(editor.Value);
        });
}
