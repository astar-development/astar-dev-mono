using System.Reflection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Versioning;

/// <inheritdoc />
public sealed class ApplicationVersionProvider : IApplicationVersionProvider
{
    /// <inheritdoc />
    public string CurrentVersion { get; }

    public ApplicationVersionProvider() : this(Assembly.GetExecutingAssembly()) { }

    internal ApplicationVersionProvider(Assembly assembly) =>
        CurrentVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.1.0";
}
