using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse Nawsome rotating five-dot loader 的 WPF 等价实现。
/// </summary>
public sealed class CheemsOrbitDotsLoader : Control
{
    private const double DesignSize = 100;
    private const double DotSize = 38.4;
    private const double DurationSeconds = 2.4;

    private long _animationStartedAt;
    private double _phase;
    private bool _renderingSubscribed;

    public static readonly DependencyProperty WhiteBrushProperty = RegisterBrush(nameof(WhiteBrush));
    public static readonly DependencyProperty RedBrushProperty = RegisterBrush(nameof(RedBrush));
    public static readonly DependencyProperty YellowBrushProperty = RegisterBrush(nameof(YellowBrush));
    public static readonly DependencyProperty GreenBrushProperty = RegisterBrush(nameof(GreenBrush));
    public static readonly DependencyProperty BlueBrushProperty = RegisterBrush(nameof(BlueBrush));

    static CheemsOrbitDotsLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsOrbitDotsLoader),
            new FrameworkPropertyMetadata(typeof(CheemsOrbitDotsLoader)));
    }

    public CheemsOrbitDotsLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public Brush? WhiteBrush
    {
        get => (Brush?)GetValue(WhiteBrushProperty);
        set => SetValue(WhiteBrushProperty, value);
    }

    public Brush? RedBrush
    {
        get => (Brush?)GetValue(RedBrushProperty);
        set => SetValue(RedBrushProperty, value);
    }

    public Brush? YellowBrush
    {
        get => (Brush?)GetValue(YellowBrushProperty);
        set => SetValue(YellowBrushProperty, value);
    }

    public Brush? GreenBrush
    {
        get => (Brush?)GetValue(GreenBrushProperty);
        set => SetValue(GreenBrushProperty, value);
    }

    public Brush? BlueBrush
    {
        get => (Brush?)GetValue(BlueBrushProperty);
        set => SetValue(BlueBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(ActualWidth / DesignSize, ActualHeight / DesignSize);
        var offsetX = (ActualWidth - (DesignSize * scale)) / 2;
        var offsetY = (ActualHeight - (DesignSize * scale)) / 2;

        drawingContext.PushTransform(new TranslateTransform(offsetX, offsetY));
        drawingContext.PushTransform(new ScaleTransform(scale, scale));
        drawingContext.PushTransform(new RotateTransform(360 * _phase, DesignSize / 2, DesignSize / 2));

        var rootSize = EvaluateRootSize(_phase);
        var rootLeft = (DesignSize - rootSize) / 2;
        var rootTop = rootLeft;

        var dotOpacity = EvaluateDotOpacity(_phase);
        var yDotWidth = EvaluateCollapsingDimension(_phase);
        var xDotHeight = EvaluateCollapsingDimension(_phase);

        // DOM painting order: .white, red, yellow, green, blue.
        DrawWhiteFlash(drawingContext, _phase);
        DrawDot(drawingContext, RedBrush, rootLeft, 50 - (DotSize / 2), yDotWidth, DotSize, dotOpacity);
        DrawDot(drawingContext, YellowBrush, 50 - (DotSize / 2), rootTop, DotSize, xDotHeight, dotOpacity);
        DrawDot(drawingContext, GreenBrush, rootLeft + rootSize - yDotWidth, 50 - (DotSize / 2), yDotWidth, DotSize, dotOpacity);
        DrawDot(drawingContext, BlueBrush, 50 - (DotSize / 2), rootTop + rootSize - xDotHeight, DotSize, xDotHeight, dotOpacity);

        drawingContext.Pop();
        drawingContext.Pop();
        drawingContext.Pop();
    }

    private void DrawWhiteFlash(DrawingContext drawingContext, double phase)
    {
        var opacity = EvaluateWhiteOpacity(phase);
        if (opacity <= 0 || WhiteBrush is null)
        {
            return;
        }

        var radiusProgress = EvaluateWhiteRadius(phase);
        var brush = WhiteBrush.CloneCurrentValue();
        brush.Opacity *= opacity;
        brush.Freeze();

        drawingContext.DrawRoundedRectangle(
            brush,
            null,
            new Rect(50 - (DotSize / 2), 50 - (DotSize / 2), DotSize, DotSize),
            (DotSize / 2) * radiusProgress,
            (DotSize / 2) * radiusProgress);
    }

    private static void DrawDot(
        DrawingContext drawingContext,
        Brush? brush,
        double x,
        double y,
        double width,
        double height,
        double opacity)
    {
        if (brush is null || width <= 0.0001 || height <= 0.0001 || opacity <= 0)
        {
            return;
        }

        var frameBrush = brush.CloneCurrentValue();
        frameBrush.Opacity *= opacity;
        frameBrush.Freeze();
        var radius = Math.Min(width, height) / 2;
        drawingContext.DrawRoundedRectangle(frameBrush, null, new Rect(x, y, width, height), radius, radius);
    }

    private static double EvaluateRootSize(double phase)
    {
        if (phase <= 0.10)
        {
            return DesignSize;
        }

        if (phase <= 0.66)
        {
            return Lerp(DesignSize, DotSize, (phase - 0.10) / 0.56);
        }

        return Lerp(DotSize, DesignSize, (phase - 0.66) / 0.34);
    }

    private static double EvaluateDotOpacity(double phase)
    {
        if (phase <= 0.66)
        {
            return Lerp(1, 0.1, phase / 0.66);
        }

        if (phase <= 0.77)
        {
            return Lerp(0.1, 1, (phase - 0.66) / 0.11);
        }

        return 1;
    }

    private static double EvaluateCollapsingDimension(double phase)
    {
        if (phase <= 0.66)
        {
            return DotSize;
        }

        if (phase <= 0.77)
        {
            return Lerp(DotSize, 0, (phase - 0.66) / 0.11);
        }

        return Lerp(0, DotSize, (phase - 0.77) / 0.23);
    }

    private static double EvaluateWhiteOpacity(double phase)
    {
        if (phase <= 0.33)
        {
            return 0;
        }

        if (phase <= 0.55)
        {
            return Lerp(0, 0.6, (phase - 0.33) / 0.22);
        }

        if (phase <= 0.66)
        {
            return Lerp(0.6, 0, (phase - 0.55) / 0.11);
        }

        return 0;
    }

    private static double EvaluateWhiteRadius(double phase)
    {
        if (phase <= 0.33)
        {
            return Lerp(1, 0, phase / 0.33);
        }

        if (phase <= 0.55)
        {
            return Lerp(0, 1, (phase - 0.33) / 0.22);
        }

        return 1;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _phase = 0;
        _animationStartedAt = Stopwatch.GetTimestamp();
        InvalidateVisual();

        SubscribeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        var elapsed = (Stopwatch.GetTimestamp() - _animationStartedAt) / (double)Stopwatch.Frequency;
        _phase = elapsed / DurationSeconds;
        _phase -= Math.Floor(_phase);
        InvalidateVisual();
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private static DependencyProperty RegisterBrush(string name)
    {
        return DependencyProperty.Register(
            name,
            typeof(Brush),
            typeof(CheemsOrbitDotsLoader),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    private static double Lerp(double start, double end, double progress) => start + ((end - start) * progress);
}
