using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class StatusLogSink(Action<string> onMessage) : ILogEventSink
{
    public void Emit(LogEvent logEvent) => onMessage(logEvent.RenderMessage(CultureInfo.InvariantCulture));
}
