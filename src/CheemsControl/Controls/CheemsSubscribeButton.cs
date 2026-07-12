using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CheemsControl;

/// <summary>Uiverse gharsh11032000 Subscribe Button 的 WPF 等价实现。</summary>
[TemplatePart(Name = "PartFill", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartShadow", Type = typeof(FrameworkElement))]
public sealed class CheemsSubscribeButton : Button
{
    private readonly ScaleTransform _buttonTransform = new(1, 1);
    private ScaleTransform? _fillTransform;
    private DropShadowEffect? _shadow;
    private long _startedAt;
    private double _buttonValue = 1, _fillValue, _colorValue;
    private double _fromButton = 1, _toButton = 1, _fromFill, _toFill, _fromColor, _toColor;
    private bool _renderingSubscribed;

    static CheemsSubscribeButton() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CheemsSubscribeButton), new FrameworkPropertyMetadata(typeof(CheemsSubscribeButton)));

    public CheemsSubscribeButton()
    {
        RenderTransform = _buttonTransform;
        RenderTransformOrigin = new Point(0.5, 0.5);
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PartFill") is FrameworkElement fill)
        {
            _fillTransform = new ScaleTransform();
            fill.RenderTransform = _fillTransform;
            fill.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        if (GetTemplateChild("PartShadow") is FrameworkElement shadow)
            _shadow = shadow.Effect as DropShadowEffect;
        ApplyVisuals();
    }

    protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); Begin(IsPressed ? 1 : 1.1, 3, 1); }
    protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); Begin(1, 0, 0); }
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnPreviewMouseLeftButtonDown(e); Begin(1, IsMouseOver ? 3 : 0, IsMouseOver ? 1 : 0); }
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) { base.OnPreviewMouseLeftButtonUp(e); Begin(IsMouseOver ? 1.1 : 1, IsMouseOver ? 3 : 0, IsMouseOver ? 1 : 0); }

    private void Begin(double button, double fill, double color)
    {
        _fromButton = _buttonValue; _toButton = button;
        _fromFill = _fillValue; _toFill = fill;
        _fromColor = _colorValue; _toColor = color;
        _startedAt = Stopwatch.GetTimestamp();
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var elapsed = (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
        var shortPhase = Math.Clamp(elapsed / 0.3, 0, 1);
        var longPhase = Math.Clamp(elapsed / 0.6, 0, 1);
        var shortEase = Bezier(shortPhase);
        _buttonValue = Lerp(_fromButton, _toButton, shortEase);
        _colorValue = Lerp(_fromColor, _toColor, shortEase);
        _fillValue = Lerp(_fromFill, _toFill, Bezier(longPhase));
        ApplyVisuals();
        if (shortPhase < 1 || longPhase < 1) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void ApplyVisuals()
    {
        _buttonTransform.ScaleX = _buttonTransform.ScaleY = _buttonValue;
        if (_fillTransform is not null) _fillTransform.ScaleX = _fillTransform.ScaleY = _fillValue;
        Foreground = new SolidColorBrush(Color.FromRgb(
            (byte)Lerp(193, 33, _colorValue), (byte)Lerp(163, 33, _colorValue), (byte)Lerp(98, 33, _colorValue)));
        if (_shadow is not null) _shadow.Opacity = 0.4 * _colorValue;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private static double Lerp(double from, double to, double p) => from + (to - from) * p;
    private static double Bezier(double x)
    {
        const double x1 = 0.23, y1 = 1, x2 = 0.32, y2 = 1;
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = Sample(t, x1, x2) - x;
            var derivative = SampleDerivative(t, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            t = Math.Clamp(t - error / derivative, 0, 1);
        }
        return Sample(t, y1, y2);
    }
    private static double Sample(double t, double a, double b) { var i = 1 - t; return 3 * i * i * t * a + 3 * i * t * t * b + t * t * t; }
    private static double SampleDerivative(double t, double a, double b) { var i = 1 - t; return 3 * i * i * a + 6 * i * t * (b - a) + 3 * t * t * (1 - b); }
}
