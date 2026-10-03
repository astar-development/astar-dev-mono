using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.Utilities;
using AStarDev.ScraperPlaying.Scoping;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Runs a scrape as the single active operation: owns its lifecycle, timing and the one report of any failure (the whole message chain, once), and leaves the choice of searches to <see cref="ISearchOrchestrator"/>.</summary>
public sealed class ScrapeService(OperationCoordinator operationCoordinator, IScopedRunner scopedRunner, ILogger<ScrapeService> logger) : IScrapeService
{
    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the single place a scrape failure is reported: every failure, including start-up ones, is reported once with its whole message chain rather than escaping.")]
    public async Task RunScraperAsync(ScrapeSelection selection, IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken))
        {
            progress.Report("An operation is already running.");

            return;
        }

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            progress.Report("Starting scrape operation.");
            LogMessage.Information(logger, "Scrape started");
            await RunWithinScopeAsync(selection, progress, cancellationToken);
            ReportCompleted(progress, startTime);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Search cancelled.");
            LogMessage.Information(logger, "Scrape cancelled");
        }
        catch (Exception e)
        {
            progress.Report($"The scrape failed: {e.ToMessageChain()}");
            LogMessage.Error(logger, "Scrape failed", e);
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    private void ReportCompleted(IProgress<string> progress, long startTime)
    {
        var duration = Stopwatch.GetElapsedTime(startTime).ToDurationString();
        progress.Report($"Search completed in: {duration}.");
        var completedMessage = $"Scrape completed in {duration}";
        LogMessage.Information(logger, completedMessage);
    }

    private async Task RunWithinScopeAsync(ScrapeSelection selection, IProgress<string> progress, CancellationToken cancellationToken)
        => _ = await scopedRunner.RunAsync<IUnitOfWork, ISearchOrchestrator, Unit>(async (unitOfWork, searchOrchestrator) =>
        {
            var configuration = await unitOfWork.LoadScrapeConfigurationAsync(cancellationToken);
            await searchOrchestrator.RunSearchesAsync(configuration, selection, progress, cancellationToken);

            return Unit.Instance;
        });
}
