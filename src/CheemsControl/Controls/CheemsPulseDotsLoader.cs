using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse adamgiebl five-dot pulse loader.
/// </summary>
public sealed class CheemsPulseDotsLoader : Control
{
    private const double DurationSeconds = 1.5;
    private const double DotDiameter = 20;
    private const double DotSpacing = 10;
    private const double LayoutWidth = (DotDiameter * 5) + (DotSpacing * 4);

    private static readonly double[] Delays = { -0.3, -0.1, 0.1, 0, 0 };
    private readonly SolidColorBrush[] _frameDotBrushes =
    {
        new(), new(), new(), new(), new()
    };

    private long _animationStartedAt;
    private bool _isRendering;

    public static readonly DependencyProperty BaseDotBrushProperty = DependencyProperty.Register(
        nameof(BaseDotBrush),
        typeof(Brush),
        typeof(CheemsPulseDotsLoader),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ActiveDotBrushProperty = DependencyProperty.Register(
        nameof(ActiveDotBrush),
        typeof(Brush),
        typeof(CheemsPulseDotsLoader),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PulseBrushProperty = DependencyProperty.Register(
        nameof(PulseBrush),
        typeof(Brush),
        typeof(CheemsPulseDotsLoader),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    static CheemsPulseDotsLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsPulseDotsLoader),
            new FrameworkPropertyMetadata(typeof(CheemsPulseDotsLoader)));
    }

    public CheemsPulseDotsLoader()
    {
        Focusable = false;
        IsHitTestVisible = false;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public Brush BaseDotBrush
    {
        get => (Brush)GetValue(BaseDotBrushProperty);
        set => SetValue(BaseDotBrushProperty, value);
    }

    public Brush ActiveDotBrush
    {
        get => (Brush)GetValue(ActiveDotBrushProperty);
        set => SetValue(ActiveDotBrushProperty, value);
    }

    public Brush PulseBrush
    {
        get => (Brush)GetValue(PulseBrushProperty);
        set => SetValue(PulseBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var baseColor = GetColor(BaseDotBrush, Color.FromRgb(0xB3, 0xD4, 0xFC));
        var activeColor = GetColor(ActiveDotBrush, Color.FromRgb(0x67, 0x93, 0xFB));
        var elapsed = SystemParameters.ClientAreaAnimation && _animationStartedAt != 0
            ? (Stopwatch.GetTimestamp() - _animationStartedAt) / (double)Stopwatch.Frequency
            : 0;

        var left = (ActualWidth - LayoutWidth) / 2;
        var centerY = ActualHeight / 2;

        for (var index = 0; index < 5; index++)
        {
            var center = new Point(left + (DotDiameter / 2) + (index * (DotDiameter + DotSpacing)), centerY);
            GetFrame(elapsed, Delays[index], baseColor, activeColor,
                out var dotScale, out var dotColor, out var pulseRadius, out var pulseOpacity);

            if (pulseOpacity > 0)
            {
                drawingContext.PushOpacity(pulseOpacity);
                drawingContext.DrawEllipse(PulseBrush, null, center, pulseRadius, pulseRadius);
                drawingContext.Pop();
            }

            _frameDotBrushes[index].Color = dotColor;
            var dotRadius = (DotDiameter / 2) * dotScale;
            drawingContext.DrawEllipse(_frameDotBrushes[index], null, center, dotRadius, dotRadius);
        }
    }

    private static void GetFrame(
        double elapsed,
        double delay,
        Color baseColor,
        Color activeColor,
        out double dotScale,
        out Color dotColor,
        out double pulseRadius,
        out double pulseOpacity)
    {
        var animationTime = elapsed - delay;
        if (animationTime < 0)
        {
            // CSS has no backwards fill mode: during the positive delay the base
            // declarations apply (scale(1), base color, and no box-shadow).
            dotScale = 1;
            dotColor = baseColor;
            pulseRadius = DotDiameter / 2;
            pulseOpacity = 0;
            return;
        }

        var phase = animationTime / DurationSeconds;
        phase -= Math.Floor(phase);

        double progress;
        if (phase <= 0.5)
        {
            progress = EaseInOut(phase / 0.5);
            dotScale = Lerp(0.8, 1.2, progress);
            dotColor = Mix(baseColor, activeColor, progress);
            pulseRadius = Lerp(10, 20, progress);
            pulseOpacity = Lerp(0.7, 0, progress);
        }
        else
        {
            progress = EaseInOut((phase - 0.5) / 0.5);
            dotScale = Lerp(1.2, 0.8, progress);
            dotColor = Mix(activeColor, baseColor, progress);
            pulseRadius = Lerp(20, 10, progress);
            pulseOpacity = Lerp(0, 0.7, progress);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animationStartedAt = Stopwatch.GetTimestamp();
        InvalidateVisual();

        if (SystemParameters.ClientAreaAnimation && !_isRendering)
        {
            CompositionTarget.Rendering += OnRendering;
            _isRendering = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            InvalidateVisual();
        }
    }

    private static double EaseInOut(double x)
    {
        var lower = 0.0;
        var upper = 1.0;
        var t = x;

        for (var index = 0; index < 12; index++)
        {
            t = (lower + upper) / 2;
            if (CubicBezier(t, 0.42, 0.58) < x)
            {
                lower = t;
            }
            else
            {
                upper = t;
            }
        }

        return CubicBezier(t, 0, 1);
    }

    private static double CubicBezier(double t, double control1, double control2)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * t * control1)
               + (3 * inverse * t * t * control2)
               + (t * t * t);
    }

    private static Color GetColor(Brush brush, Color fallback) =>
        brush is SolidColorBrush solid ? solid.Color : fallback;

    private static Color Mix(Color from, Color to, double progress) => Color.FromArgb(
        MixChannel(from.A, to.A, progress),
        MixChannel(from.R, to.R, progress),
        MixChannel(from.G, to.G, progress),
        MixChannel(from.B, to.B, progress));

    private static byte MixChannel(byte from, byte to, double progress) =>
        (byte)Math.Round(Lerp(from, to, progress));

    private static double Lerp(double from, double to, double progress) =>
        from + ((to - from) * progress);
}
