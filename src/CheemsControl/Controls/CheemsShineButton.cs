using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse mobinkakei Shine Button 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartShineName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartContentName, Type = typeof(FrameworkElement))]
public sealed class CheemsShineButton : Button
{
    private const string PartShineName = "PartShine";
    private const string PartContentName = "PartContent";
    private const double ButtonDuration = 0.7;
    private const double ContentDelay = 0.06;
    private const double ShineDuration = 0.55;

    private RotateTransform? _buttonRotation;
    private TranslateTransform? _contentTranslation;
    private TranslateTransform? _shineTranslation;
    private long _buttonStartedAt;
    private long _shineStartedAt;
    private double _shineFrom;
    private double _shineTo;
    private bool _buttonAnimating;
    private bool _shineAnimating;
    private bool _renderingSubscribed;

    static CheemsShineButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsShineButton),
            new FrameworkPropertyMetadata(typeof(CheemsShineButton)));
    }

    public CheemsShineButton()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _buttonRotation = new RotateTransform();
        RenderTransform = _buttonRotation;
        RenderTransformOrigin = new Point(0.5, 0.5);

        if (GetTemplateChild(PartContentName) is FrameworkElement content)
        {
            _contentTranslation = new TranslateTransform();
            content.RenderTransform = _contentTranslation;
        }

        if (GetTemplateChild(PartShineName) is FrameworkElement shine)
        {
            _shineTranslation = new TranslateTransform();
            shine.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new RotateTransform(35, 25, 77.5),
                    _shineTranslation
                }
            };
        }

        ResetVisuals();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (!IsEnabled || !SystemParameters.ClientAreaAnimation) return;

        var now = Stopwatch.GetTimestamp();
        _buttonStartedAt = now;
        _buttonAnimating = true;
        BeginShineTransition(now, ActualWidth * 1.2 + 75);
        SubscribeRendering();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _buttonAnimating = false;
        if (_buttonRotation is not null) _buttonRotation.Angle = 0;
        if (_contentTranslation is not null) _contentTranslation.X = 0;

        if (SystemParameters.ClientAreaAnimation)
        {
            var now = Stopwatch.GetTimestamp();
            BeginShineTransition(now, 0);
            SubscribeRendering();
        }
        else
        {
            ResetVisuals();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ResetVisuals();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void BeginShineTransition(long now, double target)
    {
        _shineFrom = _shineTranslation?.X ?? 0;
        _shineTo = target;
        _shineStartedAt = now;
        _shineAnimating = true;
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();

        if (_buttonAnimating)
        {
            var elapsed = (now - _buttonStartedAt) / (double)Stopwatch.Frequency;
            var phase = Math.Clamp(elapsed / ButtonDuration, 0, 1);
            if (_buttonRotation is not null)
            {
                _buttonRotation.Angle = EvaluateKeyframes(phase,
                    (0.00, 0), (0.25, 3), (0.50, -3), (0.75, 1), (1.00, 0));
            }

            var contentPhase = Math.Clamp((elapsed - ContentDelay) / ButtonDuration, 0, 1);
            if (_contentTranslation is not null)
            {
                _contentTranslation.X = elapsed < ContentDelay ? 0 : EvaluateKeyframes(contentPhase,
                    (0.00, 0), (0.25, 4), (0.50, -3), (0.75, 2), (1.00, 0));
            }

            if (phase >= 1)
            {
                _buttonAnimating = false;
            }
        }

        if (_shineAnimating)
        {
            var elapsed = (now - _shineStartedAt) / (double)Stopwatch.Frequency;
            var phase = Math.Clamp(elapsed / ShineDuration, 0, 1);
            var eased = CubicBezier(phase, 0.19, 1, 0.22, 1);
            if (_shineTranslation is not null)
            {
                _shineTranslation.X = _shineFrom + (_shineTo - _shineFrom) * eased;
            }
            if (phase >= 1) _shineAnimating = false;
        }

        if (!_buttonAnimating && !_shineAnimating)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderingSubscribed = false;
        }
    }

    private void ResetVisuals()
    {
        _buttonAnimating = false;
        _shineAnimating = false;
        if (_buttonRotation is not null) _buttonRotation.Angle = 0;
        if (_contentTranslation is not null) _contentTranslation.X = 0;
        if (_shineTranslation is not null) _shineTranslation.X = 0;
    }

    private static double EvaluateKeyframes(double phase, params (double Time, double Value)[] frames)
    {
        for (var index = 1; index < frames.Length; index++)
        {
            var right = frames[index];
            if (phase > right.Time) continue;
            var left = frames[index - 1];
            var local = (phase - left.Time) / (right.Time - left.Time);
            local = CubicBezier(local, 0.42, 0, 0.58, 1);
            return left.Value + (right.Value - left.Value) * local;
        }
        return frames[^1].Value;
    }

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        var parameter = x;
        for (var index = 0; index < 8; index++)
        {
            var error = Sample(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            parameter = Math.Clamp(parameter - error / derivative, 0, 1);
        }
        return Sample(parameter, y1, y2);
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
