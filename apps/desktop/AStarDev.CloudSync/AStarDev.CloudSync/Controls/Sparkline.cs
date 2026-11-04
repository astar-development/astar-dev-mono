using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AStarDev.CloudSync.Controls;

/// <summary>Draws a line through values scaled from 0 to 1, spread evenly across the control's width.</summary>
public sealed class Sparkline : Control
{
    /// <summary>Defines the <see cref="Values"/> property.</summary>
    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty = AvaloniaProperty.Register<Sparkline, IReadOnlyList<double>?>(nameof(Values));

    /// <summary>Defines the <see cref="Stroke"/> property.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke));

    private const double StrokeThickness = 2;

    static Sparkline() => AffectsRender<Sparkline>(ValuesProperty, StrokeProperty);

    /// <summary>The values to draw, each from 0 (bottom) to 1 (top). Fewer than two values draw nothing.</summary>
    public IReadOnlyList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>The line's brush.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (Values is not { Count: >= 2 } values || Stroke is null || Bounds.Width <= 0 || Bounds.Height <= StrokeThickness)
            return;

        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(PointAt(values, 0), isFilled: false);
            for (int index = 1; index < values.Count; index++)
                figure.LineTo(PointAt(values, index));

            figure.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, new Pen(Stroke, StrokeThickness, lineJoin: PenLineJoin.Round), geometry);
    }

    private Point PointAt(IReadOnlyList<double> values, int index)
    {
        double usableHeight = Bounds.Height - StrokeThickness;
        double x = Bounds.Width * index / (values.Count - 1);
        double y = (StrokeThickness / 2) + (usableHeight * (1 - Math.Clamp(values[index], 0, 1)));

        return new Point(x, y);
    }
}
