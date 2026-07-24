using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Zadrus Domino Loader 的 WPF 等价实现。</summary>
public sealed class CheemsDominoLoader : Control
{
    private const double Duration = 1.0;
    private static readonly double[] Delays = { 0.325, 0.5, 0.625, 0.74, 0.865 };
    private readonly FrameworkElement?[] _dominoes = new FrameworkElement?[5];
    private readonly RotateTransform?[] _rotations = new RotateTransform?[5];
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsDominoLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsDominoLoader),
            new FrameworkPropertyMetadata(typeof(CheemsDominoLoader)));
    }

    public CheemsDominoLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var index = 0; index < 5; index++)
        {
            var domino = GetTemplateChild($"PartDomino{index + 1}") as FrameworkElement;
            _dominoes[index] = domino;
            if (domino is null) continue;
            var rotation = new RotateTransform();
            domino.RenderTransform = rotation;
            domino.RenderTransformOrigin = new Point(0.5, 0.5);
            _rotations[index] = rotation;
        }
        ApplyFrame(0);
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
        for (var index = 0; index < 5; index++)
        {
            var domino = _dominoes[index];
            var rotation = _rotations[index];
            if (domino is null || rotation is null) continue;
            var active = seconds - Delays[index];
            if (active < 0)
            {
                domino.Opacity = 1;
                rotation.Angle = 0;
                continue;
            }

            var phase = active / Duration;
            phase -= Math.Floor(phase);
            rotation.Angle = Evaluate(phase, (0.00, 0), (0.75, 90), (1.00, 0));
            domino.Opacity = Evaluate(phase, (0.00, 1), (0.50, 0.7), (0.80, 1), (1.00, 1));
        }
    }

    private static double Evaluate(double phase, params (double Time, double Value)[] frames)
    {
        for (var index = 1; index < frames.Length; index++)
        {
            if (phase > frames[index].Time) continue;
            var left = frames[index - 1];
            var right = frames[index];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            progress = CubicBezier(progress, 0.25, 0.1, 0.25, 1);
            return left.Value + (right.Value - left.Value) * progress;
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
