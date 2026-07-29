using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.Clock.Controls;

[ExcludeFromCodeCoverage]
public class AnalogClockControl : Control
{
    private readonly DispatcherTimer _timer;

    private static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<AnalogClockControl, IBrush?>(nameof(Foreground), Brushes.Black);

    /// <summary>Defines the <see cref="ShowSecondHand" /> property.</summary>
    public static readonly StyledProperty<bool> ShowSecondHandProperty =
        AvaloniaProperty.Register<AnalogClockControl, bool>(nameof(ShowSecondHand), true);

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the second hand is drawn on the clock face.</summary>
    public bool ShowSecondHand
    {
        get => GetValue(ShowSecondHandProperty);
        set => SetValue(ShowSecondHandProperty, value);
    }

    public AnalogClockControl()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowSecondHandProperty) InvalidateVisual();
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
        double radius = Math.Min(bounds.Width, bounds.Height) * 0.45;

        bool isDark = ActualThemeVariant == ThemeVariant.Dark || Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        var foreground = Foreground ?? (isDark ? Brushes.White : Brushes.Black);

        var faceBrush = isDark ? new SolidColorBrush(Color.FromUInt32(0xFF22252A)) : new SolidColorBrush(Color.FromUInt32(0xFFFFFFFF));
        var facePen = new Pen(isDark ? new SolidColorBrush(Color.FromUInt32(0xFF444A52)) : new SolidColorBrush(Color.FromUInt32(0xFFCCCCCC)), 2);
        var tickPen = new Pen(foreground, 2);
        var minorTickPen = new Pen(foreground);

        DrawClockFace(context, center, radius, faceBrush, facePen);

        DrawTicks(context, center, radius, tickPen, minorTickPen);

        DrawHoursNumbers(context, center, radius, foreground);

        var now = DateTime.Now;
        double sec = now.Second + now.Millisecond / 1000.0;
        double min = now.Minute + sec / 60.0;
        double hour = now.Hour % 12 + min / 60.0;

        DrawHand(context, center, radius * 0.55, hour / 12.0, 5, Brushes.Red);
        DrawHand(context, center, radius * 0.75, min / 60.0, 3, Brushes.Blue);
        if (ShowSecondHand) DrawHand(context, center, radius * 0.85, sec / 60.0, 1.5, isDark ? Brushes.White : Brushes.Black);

        // Center cap
        context.DrawGeometry(isDark ? Brushes.OrangeRed : Brushes.Crimson, null, new EllipseGeometry(new Rect(center.X - 3, center.Y - 3, 6, 6)));
    }

    private static void DrawHoursNumbers(DrawingContext context, Point center, double radius, IBrush foreground)
    {
        double numberRadius = radius * 0.66;
        var numberTypeface = new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.SemiBold);
        double numberSize = ScaleSizeClampedToReadable(radius);
        var culture = CultureInfo.CurrentUICulture;
        const FlowDirection flow = FlowDirection.LeftToRight;
        for (int h = 1; h <= 12; h++)
        {
            double angle = Math.PI * 2 * (h / 12.0) - Math.PI / 2;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            var pos = new Point(center.X + cos * numberRadius, center.Y + sin * numberRadius);

            string text = h.ToString(culture);
            var ft = new FormattedText(text, culture, flow, numberTypeface, numberSize, foreground);
            double approxWidth = (text.Length == 1 ? 0.6 : 0.9) * numberSize;
            double approxHeight = numberSize;
            var origin = new Point(pos.X - approxWidth / 2.0, pos.Y - approxHeight / 2.0);
            context.DrawText(ft, origin);
        }
    }

    private static double ScaleSizeClampedToReadable(double radius) => Math.Max(10, radius * 0.12);

    private static void DrawClockFace(DrawingContext context, Point center, double radius, SolidColorBrush faceBrush, Pen facePen) => context.DrawEllipse(faceBrush, facePen, center, radius, radius);

    private static void DrawTicks(DrawingContext context, Point center, double radius, Pen tickPen, Pen minorTickPen)
    {
        for (int i = 0; i < 60; i++)
        {
            double angle = SetAngleWith12AtTheTop(i);
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            var outer = new Point(center.X + cos * radius * 0.95, center.Y + sin * radius * 0.95);
            double innerLen = (i % 5 == 0) ? 0.78 : 0.88;
            var inner = new Point(center.X + cos * radius * innerLen, center.Y + sin * radius * innerLen);
            context.DrawLine(i % 5 == 0 ? tickPen : minorTickPen, inner, outer);
        }
    }

    private static double SetAngleWith12AtTheTop(int i) => Math.PI * 2 * (i / 60.0) - Math.PI / 2;

    private static void DrawHand(DrawingContext ctx, Point center, double length, double unit, double thickness, IBrush brush)
    {
        double angle = unit * Math.PI * 2 - Math.PI / 2;
        var end = new Point(center.X + Math.Cos(angle) * length, center.Y + Math.Sin(angle) * length);
        var pen = new Pen(brush, thickness, lineCap: PenLineCap.Round);
        ctx.DrawLine(pen, center, end);
    }
}
