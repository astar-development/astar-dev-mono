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
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder loggingBuilder, string directory, string fileNamePrefix) => loggingBuilder.AddFileLogging(directory, fileNamePrefix, null);

    /// <summary>Adds a provider that writes the entries <paramref name="admit"/> allows to a daily local file.</summary>
    /// <param name="loggingBuilder">The logging builder to add the provider to.</param>
    /// <param name="directory">The directory the log files are written to.</param>
    /// <param name="fileNamePrefix">The start of every log file name.</param>
    /// <param name="admit">Decides, from an entry's level and the values of its log scopes, whether it is written; <see langword="null"/> writes every entry.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder loggingBuilder, string directory, string fileNamePrefix, Func<LogLevel, IReadOnlyDictionary<string, object?>, bool>? admit)
    {
        _ = loggingBuilder.Services.AddSingleton<ILoggerProvider>(_ => new FileLoggerProvider(directory, fileNamePrefix, TimeProvider.System, admit));

        return loggingBuilder;
    }
}
