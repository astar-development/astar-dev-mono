using System.Globalization;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.Utilities;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// Collects the user-facing status messages shown in the main window and logs the errors among them. It knows nothing about controls:
/// the window listens for <see cref="RefreshRequired"/> and renders <see cref="Text"/>. Safe to use from any thread.
/// </summary>
public sealed class StatusReporter
{
    private const int MaximumMessages = 100;
    private const string TimestampFormat = "HH:mm:ss";
    private readonly StatusMessageLog log = new(MaximumMessages);
    private readonly ILogger<StatusReporter> logger;
    private readonly TimeProvider timeProvider;

    /// <summary>Creates the reporter.</summary>
    /// <param name="logger">The logger errors are written to.</param>
    /// <param name="timeProvider">The clock every message is stamped from.</param>
    /// <param name="backoffNotifier">Announces rate limit back-offs, which are shown to the user with their delay.</param>
    public StatusReporter(ILogger<StatusReporter> logger, TimeProvider timeProvider, IRateLimitBackoffNotifier backoffNotifier)
    {
        this.logger = logger;
        this.timeProvider = timeProvider;
        backoffNotifier.BackingOff += (_, delay) => Append($"Rate limited by Wallhaven (429) - waiting {Math.Ceiling(delay.TotalSeconds)}s before retrying.");
    }

    /// <summary>Raised when the displayed text is out of date; once per burst of messages until <see cref="Text"/> is next read.</summary>
    public event EventHandler? RefreshRequired;

    /// <summary>The retained messages, newline-separated, oldest first.</summary>
    public string Text => log.Text;

    /// <summary>Appends a message to the status.</summary>
    /// <param name="message">The message to show the user; it is prefixed with the time it was appended.</param>
    public void Append(string message)
    {
        if (log.Append($"{timeProvider.GetLocalNow().ToString(TimestampFormat, CultureInfo.InvariantCulture)} {message}")) RefreshRequired?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Logs a failure and tells the user about it.</summary>
    /// <param name="message">What could not be done.</param>
    /// <param name="exception">Why it could not be done.</param>
    public void Error(string message, Exception exception)
    {
        LogMessage.Error(logger, message, exception);
        Append($"{message} {exception.ToMessageChain()}");
    }
}
