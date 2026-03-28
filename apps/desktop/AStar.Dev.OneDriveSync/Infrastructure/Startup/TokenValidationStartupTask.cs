namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class TokenValidationStartupTask : IStartupTask
{
    public const string TaskName = "TokenValidation";

    string IStartupTask.Name => TaskName;

    // Stub — token validation will be implemented by the authentication feature story
    public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
}
