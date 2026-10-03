using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingExtensions;

/// <summary>Registers the <see cref="FileLoggerProvider"/> with the logging builder.</summary>
public static class FileLoggingBuilderExtensions
{
    /// <summary>Adds a provider that writes the log to a daily local file.</summary>
    /// <param name="loggingBuilder">The logging builder to add the provider to.</param>
    /// <param name="directory">The directory the log files are written to.</param>
    /// <param name="fileNamePrefix">The start of every log file name.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder loggingBuilder, string directory, string fileNamePrefix)
    {
        _ = loggingBuilder.Services.AddSingleton<ILoggerProvider>(_ => new FileLoggerProvider(directory, fileNamePrefix, TimeProvider.System));

        return loggingBuilder;
    }
}
