using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse cssbuttons-io 金属手柄开关的 WPF 等价实现。
/// </summary>
public sealed class CheemsMetalSwitch : ToggleButton
{
    private const double HandleOffLeft = -10.0;
    private const double HandleOnLeft = 40.0;
    private const double HandleTransitionSeconds = 0.4;
    private const double BackgroundTransitionSeconds = 0.5;

    private static readonly Color OffColor = Color.FromRgb(0xD1, 0x36, 0x13);
    private static readonly Color OnColor = Color.FromRgb(0x13, 0xD1, 0x62);

    private static readonly DependencyPropertyKey HandleLeftPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HandleLeft),
            typeof(double),
            typeof(CheemsMetalSwitch),
            new FrameworkPropertyMetadata(HandleOffLeft));

    private static readonly DependencyPropertyKey TrackColorPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TrackColor),
            typeof(Color),
            typeof(CheemsMetalSwitch),
            new FrameworkPropertyMetadata(OffColor));

    public static readonly DependencyProperty HandleLeftProperty = HandleLeftPropertyKey.DependencyProperty;
    public static readonly DependencyProperty TrackColorProperty = TrackColorPropertyKey.DependencyProperty;

    private bool _isRendering;
    private long _transitionStartedAt;
    private double _handleFrom;
    private double _handleTo;
    private Color _colorFrom;
    private Color _colorTo;

    static CheemsMetalSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsMetalSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsMetalSwitch)));
    }

    public CheemsMetalSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>手柄相对于 65px 轨道左边缘的位置。</summary>
    public double HandleLeft => (double)GetValue(HandleLeftProperty);

    /// <summary>轨道当前颜色。</summary>
    public Color TrackColor => (Color)GetValue(TrackColorProperty);

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        BeginStateTransition();
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        BeginStateTransition();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
        ApplyFinalState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
    }

    private void BeginStateTransition()
    {
        var isChecked = IsChecked == true;
        var handleTarget = isChecked ? HandleOnLeft : HandleOffLeft;
        var colorTarget = isChecked ? OnColor : OffColor;

        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            StopRendering();
            SetValue(HandleLeftPropertyKey, handleTarget);
            SetValue(TrackColorPropertyKey, colorTarget);
            return;
        }

        _handleFrom = HandleLeft;
        _handleTo = handleTarget;
        _colorFrom = TrackColor;
        _colorTo = colorTarget;
        _transitionStartedAt = Stopwatch.GetTimestamp();

        if (_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var elapsed = (Stopwatch.GetTimestamp() - _transitionStartedAt) / (double)Stopwatch.Frequency;

        var handleProgress = Math.Clamp(elapsed / HandleTransitionSeconds, 0.0, 1.0);
        var handleEase = CubicBezier(handleProgress, 0.25, 0.1, 0.25, 1.0);
        SetValue(HandleLeftPropertyKey, Lerp(_handleFrom, _handleTo, handleEase));

        var colorProgress = Math.Clamp(elapsed / BackgroundTransitionSeconds, 0.0, 1.0);
        var colorEase = CubicBezier(colorProgress, 0.25, 0.1, 0.25, 1.0);
        SetValue(TrackColorPropertyKey, Lerp(_colorFrom, _colorTo, colorEase));

        if (colorProgress < 1.0)
        {
            return;
        }

        SetValue(HandleLeftPropertyKey, _handleTo);
        SetValue(TrackColorPropertyKey, _colorTo);
        StopRendering();
    }

    private void ApplyFinalState()
    {
        var isChecked = IsChecked == true;
        SetValue(HandleLeftPropertyKey, isChecked ? HandleOnLeft : HandleOffLeft);
        SetValue(TrackColorPropertyKey, isChecked ? OnColor : OffColor);
    }

    private void StopRendering()
    {
        if (!_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;
    }

    private static Color Lerp(Color from, Color to, double progress) =>
        Color.FromArgb(
            Lerp(from.A, to.A, progress),
            Lerp(from.R, to.R, progress),
            Lerp(from.G, to.G, progress),
            Lerp(from.B, to.B, progress));

    private static byte Lerp(byte from, byte to, double progress) =>
        (byte)Math.Round(from + (to - from) * progress);

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        var parameter = x;

        for (var index = 0; index < 8; index++)
        {
            var error = SampleCurve(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001)
            {
                break;
            }

            parameter = Math.Clamp(parameter - error / derivative, 0.0, 1.0);
        }

        var low = 0.0;
        var high = 1.0;
        for (var index = 0; index < 12; index++)
        {
            var sampled = SampleCurve(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001)
            {
                break;
            }

            if (sampled < x)
            {
                low = parameter;
            }
            else
            {
                high = parameter;
            }

            parameter = (low + high) * 0.5;
        }

        return SampleCurve(parameter, y1, y2);
    }

    private static double SampleCurve(double time, double first, double second)
    {
        var inverse = 1 - time;
        return 3 * inverse * inverse * time * first
             + 3 * inverse * time * time * second
             + time * time * time;
    }

    private static double SampleDerivative(double time, double first, double second)
    {
        var inverse = 1 - time;
        return 3 * inverse * inverse * first
             + 6 * inverse * time * (second - first)
             + 3 * time * time * (1 - second);
    }
}

/// <summary>
/// 复现 CSS repeating-radial-gradient 与 conic-gradient 的 37px 金属手柄表面。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CheemsMetalHandleChrome : FrameworkElement
{
    private const double OuterSize = 37.0;
    private const double BackgroundSize = 35.0;
    private const double Center = OuterSize / 2.0;
    private const int ConicSegments = 720;

    private static readonly GradientStopValue[] ConicStops =
    {
        new(0.00, Colors.White),
        new(0.10, Colors.Silver),
        new(0.35, Colors.White),
        new(0.45, Colors.Silver),
        new(0.60, Colors.White),
        new(0.70, Colors.Silver),
        new(0.80, Colors.White),
        new(0.95, Colors.Silver),
        new(1.00, Colors.White)
    };
    private static readonly Drawing HandleDrawing = CreateHandleDrawing();

    protected override Size MeasureOverride(Size availableSize) => new(OuterSize, OuterSize);

    protected override Size ArrangeOverride(Size finalSize) => new(OuterSize, OuterSize);

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawDrawing(HandleDrawing);
    }

    private static Drawing CreateHandleDrawing()
    {
        var group = new DrawingGroup();
        using (var drawing = group.Open())
        {
            drawing.PushClip(new EllipseGeometry(new Rect(1, 1, BackgroundSize, BackgroundSize)));

            var center = new Point(Center, Center);

            // CSS 的第二层 conic-gradient 没有透明色标，整个 35×35 背景必须完全不透明。
            // 先铺满不透明白色底盘，消除离散扇形抗锯齿接缝透出下方轨道的问题。
            var opaqueBaseBrush = new SolidColorBrush(Colors.White);
            opaqueBaseBrush.Freeze();
            drawing.DrawEllipse(
                opaqueBaseBrush,
                null,
                center,
                BackgroundSize / 2.0,
                BackgroundSize / 2.0);

            const double wedgeRadius = 27.0;
            for (var segment = 0; segment < ConicSegments; segment++)
            {
                var startTurn = segment / (double)ConicSegments;
                var endTurn = (segment + 1.02) / ConicSegments;
                var middleTurn = (segment + 0.5) / ConicSegments;
                var geometry = CreateWedge(center, wedgeRadius, startTurn, endTurn);
                var brush = new SolidColorBrush(GetConicColor(middleTurn));
                brush.Freeze();
                drawing.DrawGeometry(brush, null, geometry);
            }

            // CSS 径向渐变以 farthest-corner 为 100%；每 3% 重复，其中前 2% 为 20% 灰色。
            var farthestCorner = Math.Sqrt(2) * BackgroundSize / 2.0;
            var period = farthestCorner * 0.03;
            var coloredWidth = farthestCorner * 0.02;
            var ringBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xC8, 0xC8, 0xC8));
            ringBrush.Freeze();
            drawing.DrawEllipse(ringBrush, null, center, coloredWidth, coloredWidth);

            var ringPen = new Pen(ringBrush, coloredWidth);
            ringPen.Freeze();
            for (var start = period; start < farthestCorner; start += period)
            {
                var radius = start + coloredWidth / 2.0;
                drawing.DrawEllipse(null, ringPen, center, radius, radius);
            }

            drawing.Pop();

            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5)), 1.0);
            borderPen.Brush.Freeze();
            borderPen.Freeze();
            drawing.DrawEllipse(null, borderPen, new Point(Center, Center), 18.0, 18.0);
        }

        group.Freeze();
        return group;
    }

    private static StreamGeometry CreateWedge(Point center, double radius, double startTurn, double endTurn)
    {
        var startAngle = startTurn * Math.PI * 2.0;
        var endAngle = endTurn * Math.PI * 2.0;
        var start = new Point(center.X + Math.Sin(startAngle) * radius, center.Y - Math.Cos(startAngle) * radius);
        var end = new Point(center.X + Math.Sin(endAngle) * radius, center.Y - Math.Cos(endAngle) * radius);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(center, true, true);
            context.LineTo(start, true, false);
            context.LineTo(end, true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Color GetConicColor(double turn)
    {
        for (var index = 1; index < ConicStops.Length; index++)
        {
            var right = ConicStops[index];
            if (turn > right.Offset)
            {
                continue;
            }

            var left = ConicStops[index - 1];
            var progress = (turn - left.Offset) / (right.Offset - left.Offset);
            return Color.FromRgb(
                Lerp(left.Color.R, right.Color.R, progress),
                Lerp(left.Color.G, right.Color.G, progress),
                Lerp(left.Color.B, right.Color.B, progress));
        }

        return Colors.White;
    }

    private static byte Lerp(byte from, byte to, double progress) =>
        (byte)Math.Round(from + (to - from) * progress);

    private readonly record struct GradientStopValue(double Offset, Color Color);
}
