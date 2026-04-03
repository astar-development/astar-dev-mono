using System;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed record StartupTaskResult(string TaskName, bool Succeeded, Exception? Error = null);
