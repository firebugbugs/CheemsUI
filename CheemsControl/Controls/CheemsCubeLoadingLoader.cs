using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>Uiverse dexter-st 七立方体 LOADING 动画的 WPF 等价实现。</summary>
public sealed class CheemsCubeLoadingLoader : Control
{
    static CheemsCubeLoadingLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCubeLoadingLoader),
            new FrameworkPropertyMetadata(typeof(CheemsCubeLoadingLoader)));
    }
}

/// <summary>按 CSS 350px perspective 逐帧投影七个 48px 六面体。</summary>
public sealed class CheemsCubeLoadingSurface : FrameworkElement
{
    private const double Duration = 2.1;
    private const double Perspective = 350;
    private const double SurfaceWidth = 384;
    private const double SurfaceHeight = 82;
    private const double ContentOffsetX = 24;
    private const double ContentOffsetY = 6;
    private const double RootHeight = 48;
    private const double CubeSize = 48;
    private static readonly char[] Letters = "LOADING".ToCharArray();
    private static readonly double[] Delays = { 0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2 };
    private static readonly int[] ZIndices = { 0, 1, 2, 3, 2, 1, 0 };
    private static readonly int[] PaintOrder = { 0, 6, 1, 5, 2, 4, 3 };
    private Color _highlight = Color.FromRgb(0x00, 0xCC, 0x44);
    private readonly Typeface _typeface = new(new FontFamily("Poppins, Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Normal);
    private long _startedAt;
    private bool _subscribed;

    public CheemsCubeLoadingSurface()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsHitTestVisible = false;
    }

    protected override Size MeasureOverride(Size availableSize) => new(SurfaceWidth, SurfaceHeight);
    protected override Size ArrangeOverride(Size finalSize) => new(SurfaceWidth, SurfaceHeight);

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, SurfaceWidth, SurfaceHeight)));
        var seconds = _startedAt == 0 ? 0 : (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;

        foreach (var cubeIndex in PaintOrder)
        {
            DrawCube(drawingContext, cubeIndex, seconds);
        }
        drawingContext.Pop();
    }

    private void DrawCube(DrawingContext dc, int index, double seconds)
    {
        var activeTime = seconds - Delays[index];
        var phase = activeTime < 0 ? 0 : PositivePhase(activeTime / Duration);
        var z = activeTime < 0 ? -2 : Evaluate(
            phase,
            new KeyFrame(0, -2), new KeyFrame(0.30, 16), new KeyFrame(0.40, -2), new KeyFrame(1, -2));
        var y = activeTime < 0 ? 0 : Evaluate(
            phase,
            new KeyFrame(0, 0), new KeyFrame(0.30, -1), new KeyFrame(0.40, 0), new KeyFrame(1, 0));

        var colorAmount = activeTime < 0 ? 0 : Evaluate(
            phase,
            new KeyFrame(0, 0), new KeyFrame(0.10, 1), new KeyFrame(0.50, 0), new KeyFrame(1, 0));
        var textAmount = activeTime < 0 ? 0 : Evaluate(
            phase,
            new KeyFrame(0, 0), new KeyFrame(0.30, 1), new KeyFrame(0.50, 0), new KeyFrame(1, 0));
        var edgeAmount = activeTime < 0 ? 0 : Evaluate(
            phase,
            new KeyFrame(0, 0), new KeyFrame(0.30, 1), new KeyFrame(0.40, 0), new KeyFrame(1, 0));

        var x0 = ContentOffsetX + index * CubeSize;
        var x1 = x0 + CubeSize;
        var y0 = ContentOffsetY + y;
        var y1 = ContentOffsetY + y + RootHeight;
        var frontZ = z + CubeSize / 2;
        var backZ = z - CubeSize / 2;

        var ftl = Project(x0, y0, frontZ);
        var ftr = Project(x1, y0, frontZ);
        var fbr = Project(x1, y1, frontZ);
        var fbl = Project(x0, y1, frontZ);
        var btl = Project(x0, y0, backZ);
        var btr = Project(x1, y0, backZ);
        var bbr = Project(x1, y1, backZ);
        var bbl = Project(x0, y1, backZ);

        var faceBrush = BrushFor(_highlight, colorAmount);
        var sideBrush = BrushFor(_highlight, colorAmount * 0.6);
        var edgeColor = edgeAmount > 0.001
            ? Color.FromArgb((byte)Math.Round(255 * edgeAmount), _highlight.R, _highlight.G, _highlight.B)
            : Color.FromArgb(0x12, 0x00, 0x00, 0x00);
        var edgePen = new Pen(new SolidColorBrush(edgeColor), edgeAmount > 0.001 ? 1.0 : 0.45);
        edgePen.Brush.Freeze(); edgePen.Freeze();

        // CSS preserve-3d 的背面剔除结果：只画真正朝向相机的侧面。
        // 左半区看到立方体右侧，右半区看到左侧；背向侧面不能留下外伸描边。
        var cubeCenterX = (x0 + x1) / 2;
        if (cubeCenterX < SurfaceWidth / 2 - 0.001)
        {
            DrawFace(dc, sideBrush, edgePen, ftr, btr, bbr, fbr);
        }
        else if (cubeCenterX > SurfaceWidth / 2 + 0.001)
        {
            DrawFace(dc, sideBrush, edgePen, btl, ftl, fbl, bbl);
        }

        // top/bottom 在原版透明背景中近乎不可见；不绘制其外轮廓，避免 edge-glow
        // 经透视后在正面上下形成向外伸出的实体线。
        DrawFace(dc, faceBrush, edgePen, ftl, ftr, fbr, fbl);

        if (textAmount <= 0.001) return;

        var factor = Perspective / (Perspective - frontZ);
        var fontSize = 28.8 * factor;
        var text = new FormattedText(
            Letters[index].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _typeface, fontSize, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var center = Project((x0 + x1) / 2, (y0 + y1) / 2, frontZ);
        var origin = new Point(center.X - text.Width / 2, center.Y - text.Height / 2);

        // filter: drop-shadow(0 14px 10px #00cc44)，以多层低透明副本近似模糊核。
        var shadowBrush = BrushFor(_highlight, textAmount * 0.22);
        var shadowText = new FormattedText(
            Letters[index].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _typeface, fontSize, shadowBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(shadowText, new Point(origin.X - 2, origin.Y + 12));
        dc.DrawText(shadowText, new Point(origin.X + 2, origin.Y + 16));
        dc.PushOpacity(textAmount);
        dc.DrawText(text, origin);
        dc.Pop();
    }

    private static void DrawFace(DrawingContext dc, Brush brush, Pen? pen, params Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], true, true);
            for (var i = 1; i < points.Length; i++) context.LineTo(points[i], true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(brush, pen, geometry);
    }

    private static Point Project(double x, double y, double z)
    {
        var factor = Perspective / (Perspective - z);
        return new Point(
            SurfaceWidth / 2 + (x - SurfaceWidth / 2) * factor,
            ContentOffsetY + RootHeight / 2 + (y - ContentOffsetY - RootHeight / 2) * factor);
    }

    private static SolidColorBrush BrushFor(Color color, double opacity)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    private static double Evaluate(double phase, params KeyFrame[] frames)
    {
        for (var i = 1; i < frames.Length; i++)
        {
            if (phase > frames[i].Time) continue;
            var left = frames[i - 1]; var right = frames[i];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            progress = CubicBezier(progress, 0.42, 0, 0.58, 1);
            return left.Value + (right.Value - left.Value) * progress;
        }
        return frames[^1].Value;
    }

    private static double PositivePhase(double value) => value - Math.Floor(value);

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        var t = Math.Clamp(x, 0, 1);
        for (var i = 0; i < 8; i++)
        {
            var error = Sample(t, x1, x2) - x; var derivative = Derivative(t, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            t = Math.Clamp(t - error / derivative, 0, 1);
        }
        var low = 0.0; var high = 1.0;
        for (var i = 0; i < 12; i++)
        {
            var sampled = Sample(t, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001) break;
            if (sampled < x) low = t; else high = t; t = (low + high) / 2;
        }
        return Sample(t, y1, y2);
    }

    private static double Sample(double t, double a, double b) { var i = 1 - t; return 3 * i * i * t * a + 3 * i * t * t * b + t * t * t; }
    private static double Derivative(double t, double a, double b) { var i = 1 - t; return 3 * i * i * a + 6 * i * t * (b - a) + 3 * t * t * (1 - b); }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (TryFindResource(CheemsKeys.CubeLoadingHighlightColor) is Color highlight)
        {
            _highlight = highlight;
        }
        _startedAt = Stopwatch.GetTimestamp();
        if (SystemParameters.ClientAreaAnimation && !_subscribed)
        {
            CompositionTarget.Rendering += OnRendering; _subscribed = true;
        }
        InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_subscribed) return;
        CompositionTarget.Rendering -= OnRendering; _subscribed = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsVisible) InvalidateVisual();
    }

    private readonly record struct KeyFrame(double Time, double Value);
}
