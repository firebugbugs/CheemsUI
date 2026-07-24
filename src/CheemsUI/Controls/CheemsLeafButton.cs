using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse MuhammadHasann 植物装饰按钮的 WPF 等价实现。</summary>
public sealed class CheemsLeafButton : Button
{
    private readonly RotateTransform?[] _rotations = new RotateTransform?[3];
    private readonly double[] _leaveFrom = new double[3];
    private long _hoverStartedAt;
    private long _leaveStartedAt;
    private bool _hoverAnimating;
    private bool _leaveAnimating;
    private bool _renderingSubscribed;

    static CheemsLeafButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsLeafButton),
            new FrameworkPropertyMetadata(typeof(CheemsLeafButton)));
    }

    public CheemsLeafButton() => Unloaded += OnUnloaded;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        AttachRotation(0, "PartIcon1", new Point(0, 0));
        AttachRotation(1, "PartIcon2", new Point(0.5, 0));
        AttachRotation(2, "PartIcon3", new Point(0.5, 0));
        SetBaseAngles();
    }

    private void AttachRotation(int index, string partName, Point origin)
    {
        if (GetTemplateChild(partName) is not FrameworkElement icon) return;
        var rotation = new RotateTransform();
        icon.RenderTransform = rotation;
        icon.RenderTransformOrigin = origin;
        _rotations[index] = rotation;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (!IsEnabled || !SystemParameters.ClientAreaAnimation) return;
        _hoverStartedAt = Stopwatch.GetTimestamp();
        _hoverAnimating = true;
        _leaveAnimating = false;
        SubscribeRendering();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverAnimating = false;
        if (!SystemParameters.ClientAreaAnimation)
        {
            SetBaseAngles();
            return;
        }

        for (var index = 0; index < 3; index++) _leaveFrom[index] = _rotations[index]?.Angle ?? 0;
        _leaveStartedAt = Stopwatch.GetTimestamp();
        _leaveAnimating = true;
        SubscribeRendering();
    }

    private void SubscribeRendering()
    {
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
        var now = Stopwatch.GetTimestamp();
        if (_hoverAnimating)
        {
            var elapsed = (now - _hoverStartedAt) / (double)Stopwatch.Frequency;
            SetAngle(0, Oscillate(elapsed / 3.0, 10, -5));

            if (elapsed < 1)
            {
                SetAngle(1, Lerp(10, 0, EaseInOut(elapsed)));
                SetAngle(2, Lerp(-5, 0, EaseInOut(elapsed)));
            }
            else
            {
                SetAngle(1, Oscillate((elapsed - 1) / 3.0, 0, 15));
                SetAngle(2, Oscillate((elapsed - 1) / 2.0, 0, -5));
            }
        }
        else if (_leaveAnimating)
        {
            var elapsed = (now - _leaveStartedAt) / (double)Stopwatch.Frequency;
            SetAngle(0, Lerp(_leaveFrom[0], 10, EaseInOut(Math.Clamp(elapsed / 0.5, 0, 1))));
            SetAngle(1, Lerp(_leaveFrom[1], 10, EaseInOut(Math.Clamp(elapsed, 0, 1))));
            SetAngle(2, Lerp(_leaveFrom[2], -5, EaseInOut(Math.Clamp(elapsed, 0, 1))));
            if (elapsed >= 1)
            {
                _leaveAnimating = false;
                SetBaseAngles();
            }
        }

        if (!_hoverAnimating && !_leaveAnimating)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderingSubscribed = false;
        }
    }

    private static double Oscillate(double rawPhase, double start, double middle)
    {
        var phase = rawPhase - Math.Floor(rawPhase);
        return phase <= 0.5
            ? Lerp(start, middle, CustomEase(phase / 0.5))
            : Lerp(middle, start, CustomEase((phase - 0.5) / 0.5));
    }

    private void SetBaseAngles()
    {
        SetAngle(0, 10);
        SetAngle(1, 10);
        SetAngle(2, -5);
    }

    private void SetAngle(int index, double angle)
    {
        if (_rotations[index] is not null) _rotations[index]!.Angle = angle;
    }

    private static double CustomEase(double x) => CubicBezier(x, 0.52, 0, 0.58, 1);
    private static double EaseInOut(double x) => CubicBezier(x, 0.42, 0, 0.58, 1);
    private static double Lerp(double from, double to, double progress) => from + (to - from) * progress;

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
