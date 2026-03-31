namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>Formats a timestamp as a locale-aware relative or absolute string (AC LO-07).</summary>
public interface IRelativeTimeFormatter
{
    /// <summary>
    ///     Returns a relative string ("<c>5 minutes ago</c>") for differences &lt; 1 hour,
    ///     or an absolute string ("<c>Today at 14:32</c>" / "<c>25 Mar at 09:15</c>") otherwise.
    /// </summary>
    string Format(DateTimeOffset timestamp, DateTimeOffset now);
}
