using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Moves a scrape configuration in or out through a file the person picks.</summary>
public sealed class ConfigurationTransfer(IScrapeConfigurationImportService importService, IScrapeConfigurationExportService exportService)
{
    /// <summary>Imports the configuration in the file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    /// <param name="cancellationToken">Cancels the import.</param>
    /// <returns>Some when a file was picked and imported; none when the person cancelled the picker.</returns>
    public async Task<Option<Unit>> ImportPickedFileAsync(IDialogHost dialogs, CancellationToken cancellationToken)
    {
        var path = await dialogs.PickImportFileAsync();

        return await path.MatchAsync(
            async selectedPath =>
            {
                await importService.ImportAsync(selectedPath, cancellationToken);

                return Option.Some(Unit.Instance);
            },
            Option.None<Unit>);
    }

    /// <summary>Exports the configuration to the file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    /// <param name="apiKeys">Whether the exported file includes the API keys.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    /// <returns>Some with whether a configuration was exported when a file was picked; none when the person cancelled the picker.</returns>
    public async Task<Option<bool>> ExportPickedFileAsync(IDialogHost dialogs, ApiKeyExport apiKeys, CancellationToken cancellationToken)
    {
        var path = await dialogs.PickExportFileAsync();

        return await path.MatchAsync(
            async selectedPath => Option.Some(await exportService.ExportAsync(selectedPath, apiKeys, cancellationToken)),
            Option.None<bool>);
    }
}
