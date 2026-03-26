using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace AStar.Dev.OneDriveSync.old.Logging;

/// <summary>
/// Text formatter that applies PII sanitisation before writing to the file sink (LG-02).
/// </summary>
public sealed class SanitisedTextFormatter : ITextFormatter
{
    private readonly MessageTemplateTextFormatter _inner = new("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var buffer = new StringWriter();
        _inner.Format(logEvent, buffer);
        output.Write(PiiSanitiser.Sanitise(buffer.ToString()));
    }
}
