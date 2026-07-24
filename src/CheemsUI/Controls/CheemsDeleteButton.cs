using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

[TemplatePart(Name = "PartSurface", Type = typeof(Border))]
[TemplatePart(Name = "PartIcon", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartLabel", Type = typeof(TextBlock))]
public sealed class CheemsDeleteButton : Button
{
    private const double Duration = 0.3;
    private Border? _surface;
    private FrameworkElement? _icon;
    private TextBlock? _label;
    private TranslateTransform? _iconTranslation;
    private TranslateTransform? _labelTranslation;
    private long _startedAt;
    private double _fromProgress;
    private double _toProgress;
    private double _currentProgress;
    private bool _renderingSubscribed;

    static CheemsDeleteButton() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CheemsDeleteButton), new FrameworkPropertyMetadata(typeof(CheemsDeleteButton)));

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _surface = GetTemplateChild("PartSurface") as Border;
        _icon = GetTemplateChild("PartIcon") as FrameworkElement;
        _label = GetTemplateChild("PartLabel") as TextBlock;
        _iconTranslation = new TranslateTransform();
        _labelTranslation = new TranslateTransform();
        if (_icon is not null) _icon.RenderTransform = _iconTranslation;
        if (_label is not null) _label.RenderTransform = _labelTranslation;
        ApplyProgress(IsMouseOver ? 1 : 0);
    }

    protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); BeginTransition(1); }
    protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); BeginTransition(0); }

    private void BeginTransition(double target)
    {
        _fromProgress = _currentProgress;
        _toProgress = target;
        _startedAt = Stopwatch.GetTimestamp();
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var elapsed = (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
        var phase = Math.Clamp(elapsed / Duration, 0, 1);
        var eased = CubicBezier(phase, 0.25, 0.1, 0.25, 1);
        ApplyProgress(_fromProgress + (_toProgress - _fromProgress) * eased);
        if (phase < 1) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void ApplyProgress(double progress)
    {
        _currentProgress = progress;
        Width = 50 + 90 * progress;
        if (_surface is not null)
            _surface.Background = new SolidColorBrush(Interpolate(Color.FromRgb(20, 20, 20), Color.FromRgb(255, 69, 69), progress));
        if (_icon is not null) _icon.Width = 12 + 38 * progress;
        if (_iconTranslation is not null) _iconTranslation.Y = 30 * progress;
        if (_label is not null) { _label.FontSize = 2 + 11 * progress; _label.Opacity = progress; }
        if (_labelTranslation is not null) _labelTranslation.Y = 30 * progress;
    }

    private static Color Interpolate(Color from, Color to, double p) => Color.FromRgb(
        (byte)(from.R + (to.R - from.R) * p), (byte)(from.G + (to.G - from.G) * p), (byte)(from.B + (to.B - from.B) * p));

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
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
