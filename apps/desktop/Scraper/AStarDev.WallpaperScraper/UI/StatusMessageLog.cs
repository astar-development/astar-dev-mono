namespace AStarDev.WallpaperScraper.UI;

/// <summary>
/// Accumulates status messages up to a fixed capacity, discarding the oldest message once that capacity is exceeded, and renders the retained messages as newline-separated text.
/// Safe to append to from any thread. The text is rebuilt only when read after an append, and the log tells the appender whether a display refresh is needed so a burst of messages costs one refresh.
/// </summary>
/// <param name="maximumMessages">The maximum number of messages to retain.</param>
public sealed class StatusMessageLog(int maximumMessages)
{
    private readonly Lock gate = new();
    private readonly Queue<string> messages = new();
    private string text = string.Empty;
    private bool isTextStale;
    private bool isRefreshPending;

    /// <summary>
    /// The retained messages, newline-separated, oldest first. Reading it acknowledges any pending display refresh, so the next
    /// <see cref="Append"/> reports that a refresh is required again.
    /// </summary>
    public string Text
    {
        get
        {
            lock (gate)
            {
                if (isTextStale)
                {
                    text = string.Join(Environment.NewLine, messages);
                    isTextStale = false;
                }

                isRefreshPending = false;

                return text;
            }
        }
    }

    /// <summary>Appends <paramref name="message"/>, discarding the oldest message if over capacity.</summary>
    /// <param name="message">The message to append.</param>
    /// <returns><c>true</c> if the caller must schedule a display refresh, or <c>false</c> if one is already pending because <see cref="Text"/> has not been read since an earlier append.</returns>
    public bool Append(string message)
    {
        lock (gate)
        {
            messages.Enqueue(message);
            while (messages.Count > maximumMessages)
            {
                _ = messages.Dequeue();
            }

            isTextStale = true;
            var refreshRequired = !isRefreshPending;
            isRefreshPending = true;

            return refreshRequired;
        }
    }
}
