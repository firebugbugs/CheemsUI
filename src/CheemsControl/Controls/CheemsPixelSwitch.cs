using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse zl306 像素风开关的 WPF 等价实现。
/// </summary>
public sealed class CheemsPixelSwitch : ToggleButton
{
    private const double CheckedOffset = 32.0;
    private const double TransitionSeconds = 0.3;

    private static readonly DependencyPropertyKey ThumbOffsetPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ThumbOffset),
            typeof(double),
            typeof(CheemsPixelSwitch),
            new FrameworkPropertyMetadata(0.0));

    public static readonly DependencyProperty ThumbOffsetProperty = ThumbOffsetPropertyKey.DependencyProperty;

    private bool _isRendering;
    private long _transitionStartedAt;
    private double _transitionFrom;
    private double _transitionTo;

    static CheemsPixelSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsPixelSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsPixelSwitch)));
    }

    public CheemsPixelSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>滑块当前的水平位移；由控件按 CSS transition 逐帧维护。</summary>
    public double ThumbOffset => (double)GetValue(ThumbOffsetProperty);

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
        SetValue(ThumbOffsetPropertyKey, IsChecked == true ? CheckedOffset : 0.0);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
    }

    private void BeginStateTransition()
    {
        var target = IsChecked == true ? CheckedOffset : 0.0;
        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            StopRendering();
            SetValue(ThumbOffsetPropertyKey, target);
            return;
        }

        _transitionFrom = ThumbOffset;
        _transitionTo = target;
        if (Math.Abs(_transitionTo - _transitionFrom) < 0.001)
        {
            StopRendering();
            SetValue(ThumbOffsetPropertyKey, target);
            return;
        }

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
        var progress = Math.Clamp(elapsed / TransitionSeconds, 0.0, 1.0);
        var eased = CubicBezier(progress, 0.25, 0.1, 0.25, 1.0);
        SetValue(ThumbOffsetPropertyKey, Lerp(_transitionFrom, _transitionTo, eased));

        if (progress < 1.0)
        {
            return;
        }

        SetValue(ThumbOffsetPropertyKey, _transitionTo);
        StopRendering();
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

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;
}
