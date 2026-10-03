using AStarDev.WallpaperScraper.Operations;
using System.Diagnostics.CodeAnalysis;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Startup;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>
/// Tracks whether the application is ready to be used: the database prepared, the root directory present and no operation running.
/// The main window enables its controls from <see cref="CanOperate"/> and <see cref="CanRunScraper"/> and re-reads them when <see cref="Changed"/> is raised.
/// </summary>
public sealed class ApplicationReadiness
{
    private readonly IDatabaseInitialization databaseInitialization;
    private readonly IRootDirectoryCheck rootDirectoryCheck;
    private readonly OperationCoordinator operationCoordinator;
    private readonly StatusReporter status;

    /// <summary>Initializes a new instance of the <see cref="ApplicationReadiness"/> class.</summary>
    /// <param name="databaseInitialization">The background database preparation to wait for.</param>
    /// <param name="rootDirectoryCheck">The check that the root directory exists.</param>
    /// <param name="operationCoordinator">Tracks whether an operation is running.</param>
    /// <param name="status">Where progress and problems are reported.</param>
    public ApplicationReadiness(IDatabaseInitialization databaseInitialization, IRootDirectoryCheck rootDirectoryCheck, OperationCoordinator operationCoordinator, StatusReporter status)
    {
        this.databaseInitialization = databaseInitialization;
        this.rootDirectoryCheck = rootDirectoryCheck;
        this.operationCoordinator = operationCoordinator;
        this.status = status;
        operationCoordinator.StateChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when <see cref="CanOperate"/> or <see cref="CanRunScraper"/> may have changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether the database is prepared.</summary>
    public bool IsDatabaseReady { get; private set; }

    /// <summary>Gets a value indicating whether the root directory exists. True until it has been checked and found missing.</summary>
    public bool IsRootDirectoryAvailable { get; private set; } = true;

    /// <summary>Gets a value indicating whether an operation is running and can be cancelled.</summary>
    public bool IsOperationRunning => operationCoordinator.IsOperationRunning;

    /// <summary>Gets a value indicating whether configuration operations may start: the database is ready and nothing is running.</summary>
    public bool CanOperate => IsDatabaseReady && !operationCoordinator.IsOperationRunning;

    /// <summary>Gets a value indicating whether the scraper may start.</summary>
    public bool CanRunScraper => CanOperate && IsRootDirectoryAvailable;

    /// <summary>Waits for the database, then checks the root directory, reporting progress and problems to the user.</summary>
    /// <returns><c>true</c> if the database is ready; otherwise <c>false</c>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The preparation task can fail with any database or I/O exception; every failure must be reported and leave the controls disabled rather than crash the window.")]
    public async Task<bool> InitialiseAsync()
    {
        status.Append("Preparing the database.");
        try
        {
            await databaseInitialization.ReadyAsync();
        }
        catch (Exception exception)
        {
            status.Error("Unable to prepare the database.", exception);

            return false;
        }

        IsDatabaseReady = true;
        status.Append("Database ready.");
        Changed?.Invoke(this, EventArgs.Empty);
        await CheckRootDirectoryAsync();

        return true;
    }

    private async Task CheckRootDirectoryAsync()
    {
        try
        {
            IsRootDirectoryAvailable = await rootDirectoryCheck.ExistsAsync();
            if (!IsRootDirectoryAvailable) status.Append("Root directory could not be found.");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            IsRootDirectoryAvailable = false;
            status.Error("Unable to check the root directory.", exception);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
