using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed partial class StartupOrchestrator(IEnumerable<IStartupTask> tasks, ILogger<StartupOrchestrator> logger)
{
    public async Task<IReadOnlyList<StartupTaskResult>> RunAsync(CancellationToken ct)
    {
        var taskList = tasks.ToList();

        LogOrchestratorStarting(logger, taskList.Count);

        IEnumerable<Task<StartupTaskResult>> resultTasks = taskList.Select(task => ExecuteTaskAsync(task, ct));
        StartupTaskResult[] results     = await Task.WhenAll(resultTasks).ConfigureAwait(false);

        return results;
    }

    private async Task<StartupTaskResult> ExecuteTaskAsync(IStartupTask task, CancellationToken ct)
    {
        LogTaskStarting(logger, task.Name);

        try
        {
            await task.RunAsync(ct).ConfigureAwait(false);

            LogTaskCompleted(logger, task.Name);

            return new StartupTaskResult(task.Name, Succeeded: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTaskFailed(logger, task.Name, ex);

            return new StartupTaskResult(task.Name, Succeeded: false, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Startup orchestrator beginning {TaskCount} task(s)")]
    private static partial void LogOrchestratorStarting(ILogger logger, int taskCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Startup task '{TaskName}' starting")]
    private static partial void LogTaskStarting(ILogger logger, string taskName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Startup task '{TaskName}' completed successfully")]
    private static partial void LogTaskCompleted(ILogger logger, string taskName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Startup task '{TaskName}' failed")]
    private static partial void LogTaskFailed(ILogger logger, string taskName, Exception ex);
}
