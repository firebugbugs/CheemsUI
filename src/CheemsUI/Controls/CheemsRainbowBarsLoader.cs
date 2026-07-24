using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Gianluks90 Rainbow Bars Loader 的 WPF 等价实现。</summary>
public sealed class CheemsRainbowBarsLoader : Control
{
    private const double Duration = 0.45;
    private static readonly double[] Delays = { 0.10, 0.20, 0.25, 0.30, 0.35, 0.40 };
    private readonly FrameworkElement?[] _bars = new FrameworkElement?[6];
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsRainbowBarsLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsRainbowBarsLoader),
            new FrameworkPropertyMetadata(typeof(CheemsRainbowBarsLoader)));
    }

    public CheemsRainbowBarsLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var index = 0; index < 6; index++)
        {
            _bars[index] = GetTemplateChild($"PartBar{index + 1}") as FrameworkElement;
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
        for (var index = 0; index < 6; index++)
        {
            var bar = _bars[index];
            if (bar is null) continue;
            var active = seconds - Delays[index];
            if (active < 0)
            {
                bar.Height = 5;
                continue;
            }

            var iteration = (int)Math.Floor(active / Duration);
            var local = active / Duration - iteration;
            var progress = iteration % 2 == 0 ? Ease(local) : Ease(1 - local);
            bar.Height = 5 + 35 * progress;
        }
    }

    private static double Ease(double x) => CubicBezier(x, 0.25, 0.1, 0.25, 1);

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
