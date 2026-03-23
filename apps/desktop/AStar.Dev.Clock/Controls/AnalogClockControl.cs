using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStar.Dev.Clock.Controls;

public class AnalogClockControl : Control
{
    private readonly DispatcherTimer _timer;

    private static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<AnalogClockControl, IBrush?>(nameof(Foreground), Brushes.Black);

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public AnalogClockControl()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => InvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_timer.IsEnabled) _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_timer.IsEnabled) _timer.Stop();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var center = bounds.Center;
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.45;

        // Theme-aware brushes
        var isDark = ActualThemeVariant == ThemeVariant.Dark
                     || Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        var foreground = Foreground ?? (isDark ? Brushes.White : Brushes.Black);

        var faceBrush = isDark ? new SolidColorBrush(Color.FromUInt32(0xFF22252A)) : new SolidColorBrush(Color.FromUInt32(0xFFFFFFFF));
        var facePen = new Pen(isDark ? new SolidColorBrush(Color.FromUInt32(0xFF444A52)) : new SolidColorBrush(Color.FromUInt32(0xFFCCCCCC)), 2);
        var tickPen = new Pen(foreground, 2);
        var minorTickPen = new Pen(foreground);

        // Draw clock face
        context.DrawEllipse(faceBrush, facePen, center, radius, radius);

        // Draw ticks
        for (var i = 0; i < 60; i++)
        {
            var angle = Math.PI * 2 * (i / 60.0) - Math.PI / 2; // start at 12 o'clock
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var outer = new Point(center.X + cos * radius * 0.95, center.Y + sin * radius * 0.95);
            var innerLen = (i % 5 == 0) ? 0.78 : 0.88; // hour ticks longer
            var inner = new Point(center.X + cos * radius * innerLen, center.Y + sin * radius * innerLen);
            context.DrawLine(i % 5 == 0 ? tickPen : minorTickPen, inner, outer);
        }

        // Draw hour numbers (1-12)
        // Keep them inside the hour ticks a bit
        var numberRadius = radius * 0.66;
        var numberTypeface = new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.SemiBold);
        var numberSize = Math.Max(10, radius * 0.12); // scale with control size, clamp to a readable minimum
        var culture = CultureInfo.CurrentUICulture;
        const FlowDirection flow = FlowDirection.LeftToRight;
        for (var h = 1; h <= 12; h++)
        {
            var angle = Math.PI * 2 * (h / 12.0) - Math.PI / 2; // 12 at the top
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var pos = new Point(center.X + cos * numberRadius, center.Y + sin * numberRadius);

            // Use FormattedText so we can measure and center precisely
            var text = h.ToString();
            var ft = new FormattedText(text, culture, flow, numberTypeface, numberSize, foreground);
            // Approximate text size since FormattedText doesn't expose Size in this API surface
            var approxWidth = (text.Length == 1 ? 0.6 : 0.9) * numberSize;
            var approxHeight = numberSize; // rough height equals font size
            // Center the text at computed position
            var origin = new Point(pos.X - approxWidth / 2.0, pos.Y - approxHeight / 2.0);
            context.DrawText(ft, origin);
        }

        // Current time
        var now = DateTime.Now;
        var sec = now.Second + now.Millisecond / 1000.0;
        var min = now.Minute + sec / 60.0;
        var hour = now.Hour % 12 + min / 60.0;

        // Hands
        DrawHand(context, center, radius * 0.55, hour / 12.0, 5, Brushes.Red);
        DrawHand(context, center, radius * 0.75, min / 60.0, 3, Brushes.Blue);
        DrawHand(context, center, radius * 0.85, sec / 60.0, 1.5, isDark ? Brushes.White : Brushes.Black);

        // Center cap
        context.DrawGeometry(isDark ? Brushes.OrangeRed : Brushes.Crimson, null,
            new EllipseGeometry(new Rect(center.X - 3, center.Y - 3, 6, 6)));
    }

    private static void DrawHand(DrawingContext ctx, Point center, double length, double unit, double thickness, IBrush brush)
    {
        var angle = unit * Math.PI * 2 - Math.PI / 2;
        var end = new Point(center.X + Math.Cos(angle) * length, center.Y + Math.Sin(angle) * length);
        var pen = new Pen(brush, thickness, lineCap: PenLineCap.Round);
        ctx.DrawLine(pen, center, end);
    }
}
