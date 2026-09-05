using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>CodePen jkantner Concentric Circle Preloader 的 WPF 等价实现。</summary>
public sealed class CheemsConcentricCircleLoader : Control
{
    public const double AnimationCycleDurationSeconds = 4.0;

    private static readonly double[] Radii = { 48, 38, 31, 26, 21, 5 };
    private static readonly double[] StrokeWidths = { 4, 3, 2, 2, 2, 2 };
    private static readonly double[] RotationEnds = { 270, 540, 135, 63, 63, 135 };

    private long _animationStartedAt;
    private bool _renderingSubscribed;

    public static readonly DependencyProperty BlueBrushProperty = RegisterBrush(nameof(BlueBrush));
    public static readonly DependencyProperty PurpleBrushProperty = RegisterBrush(nameof(PurpleBrush));
    public static readonly DependencyProperty PinkBrushProperty = RegisterBrush(nameof(PinkBrush));
    public static readonly DependencyProperty YellowBrushProperty = RegisterBrush(nameof(YellowBrush));
    public static readonly DependencyProperty PaleBlueBrushProperty = RegisterBrush(nameof(PaleBlueBrush));
    public static readonly DependencyProperty CoreBrushProperty = RegisterBrush(nameof(CoreBrush));

    static CheemsConcentricCircleLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsConcentricCircleLoader),
            new FrameworkPropertyMetadata(typeof(CheemsConcentricCircleLoader)));
    }

    public CheemsConcentricCircleLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public Brush BlueBrush { get => (Brush)GetValue(BlueBrushProperty); set => SetValue(BlueBrushProperty, value); }
    public Brush PurpleBrush { get => (Brush)GetValue(PurpleBrushProperty); set => SetValue(PurpleBrushProperty, value); }
    public Brush PinkBrush { get => (Brush)GetValue(PinkBrushProperty); set => SetValue(PinkBrushProperty, value); }
    public Brush YellowBrush { get => (Brush)GetValue(YellowBrushProperty); set => SetValue(YellowBrushProperty, value); }
    public Brush PaleBlueBrush { get => (Brush)GetValue(PaleBlueBrushProperty); set => SetValue(PaleBlueBrushProperty, value); }
    public Brush CoreBrush { get => (Brush)GetValue(CoreBrushProperty); set => SetValue(CoreBrushProperty, value); }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        var scale = Math.Min(ActualWidth, ActualHeight) / 100.0;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var phase = _animationStartedAt == 0
            ? 0
            : (Stopwatch.GetTimestamp() - _animationStartedAt) /
              (double)Stopwatch.Frequency / AnimationCycleDurationSeconds;
        phase -= Math.Floor(phase);

        DrawCore(drawingContext, center, scale, phase);
        var brushes = new[] { BlueBrush, PurpleBrush, PinkBrush, YellowBrush, BlueBrush, PaleBlueBrush };
        for (var index = 0; index < Radii.Length; index++)
        {
            DrawRing(drawingContext, center, scale, phase, index, brushes[index]);
        }
    }

    private void DrawCore(DrawingContext context, Point center, double scale, double phase)
    {
        var halfPhase = phase < 0.5 ? phase * 2 : (phase - 0.5) * 2;
        var eased = CubicBezier(halfPhase, 0.65, 0, 0.35, 1);
        var firstScale = phase < 0.5 ? eased : 1 - eased;
        var secondScale = 1 - firstScale;
        context.DrawEllipse(CoreBrush, null, center, 15 * scale * secondScale, 15 * scale * secondScale);
        context.DrawEllipse(CoreBrush, null, center, 15 * scale * firstScale, 15 * scale * firstScale);
    }

    private static void DrawRing(
        DrawingContext context, Point center, double scale, double phase, int index, Brush brush)
    {
        var halfPhase = phase < 0.5 ? phase * 2 : (phase - 0.5) * 2;
        var eased = CubicBezier(halfPhase, 0.65, 0, 0.35, 1);
        var visibleFraction = phase < 0.5 ? 1 - eased : eased;
        var rotation = RotationEnds[index] * phase;
        var dashShift = 360 * eased;
        var startAngle = -90 + rotation + dashShift;
        var radius = Radii[index] * scale;
        var pen = new Pen(brush, StrokeWidths[index] * scale)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat
        };

        if (visibleFraction >= 0.999)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        if (visibleFraction <= 0.001) return;
        var sweep = 360 * visibleFraction;
        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(PointOnCircle(center, radius, startAngle), false, false);
            sink.ArcTo(
                PointOnCircle(center, radius, startAngle + sweep),
                new Size(radius, radius),
                0,
                sweep > 180,
                SweepDirection.Clockwise,
                true,
                false);
        }
        geometry.Freeze();
        context.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animationStartedAt = Stopwatch.GetTimestamp();
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsVisible) InvalidateVisual();
    }

    private static DependencyProperty RegisterBrush(string name) => DependencyProperty.Register(
        name,
        typeof(Brush),
        typeof(CheemsConcentricCircleLoader),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        var t = Math.Clamp(x, 0, 1);
        for (var index = 0; index < 8; index++)
        {
            var error = Sample(t, x1, x2) - x;
            var derivative = SampleDerivative(t, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            t = Math.Clamp(t - error / derivative, 0, 1);
        }
        return Sample(t, y1, y2);
    }

    private static double Sample(double t, double first, double second)
    {
        var inverse = 1 - t;
        return 3 * inverse * inverse * t * first + 3 * inverse * t * t * second + t * t * t;
    }

    private static double SampleDerivative(double t, double first, double second)
    {
        var inverse = 1 - t;
        return 3 * inverse * inverse * first + 6 * inverse * t * (second - first) + 3 * t * t * (1 - second);
    }
}
