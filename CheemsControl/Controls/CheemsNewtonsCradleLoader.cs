using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>Uiverse dovatgabriel Newton's Cradle Loader 的 WPF 等价实现。</summary>
public sealed class CheemsNewtonsCradleLoader : Control
{
    private const double Duration = 1.2;
    private RotateTransform? _firstRotation;
    private RotateTransform? _lastRotation;
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsNewtonsCradleLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsNewtonsCradleLoader),
            new FrameworkPropertyMetadata(typeof(CheemsNewtonsCradleLoader)));
    }

    public CheemsNewtonsCradleLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _firstRotation = AttachRotation(GetTemplateChild("PartFirstDot") as FrameworkElement);
        _lastRotation = AttachRotation(GetTemplateChild("PartLastDot") as FrameworkElement);
        ApplyFrame(0);
    }

    private static RotateTransform? AttachRotation(FrameworkElement? element)
    {
        if (element is null) return null;
        var rotation = new RotateTransform();
        element.RenderTransform = rotation;
        element.RenderTransformOrigin = new Point(0.5, 0);
        return rotation;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startedAt = Stopwatch.GetTimestamp();
        ApplyFrame(0);
        if (SystemParameters.ClientAreaAnimation && !_renderingSubscribed)
        {
            CompositionTarget.Rendering += OnRendering;
            _renderingSubscribed = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsVisible) return;
        var seconds = (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
        ApplyFrame(seconds);
    }

    private void ApplyFrame(double seconds)
    {
        var phase = seconds / Duration;
        phase -= Math.Floor(phase);

        if (_firstRotation is not null)
        {
            _firstRotation.Angle = phase switch
            {
                <= 0.25 => 70 * EaseOut(phase / 0.25),
                <= 0.50 => 70 * (1 - EaseIn((phase - 0.25) / 0.25)),
                _ => 0
            };
        }

        if (_lastRotation is not null)
        {
            _lastRotation.Angle = phase switch
            {
                <= 0.50 => 0,
                <= 0.75 => -70 * EaseOut((phase - 0.50) / 0.25),
                _ => -70 * (1 - EaseIn((phase - 0.75) / 0.25))
            };
        }
    }

    private static double EaseIn(double x) => CubicBezier(x, 0.42, 0, 1, 1);
    private static double EaseOut(double x) => CubicBezier(x, 0, 0, 0.58, 1);

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
