using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>Uiverse Satwinder04 Loader3 音频柱 Loader 的 WPF 等价实现。</summary>
public sealed class CheemsWaveBarsLoader : Control
{
    private const double Duration = 3.0;
    private static readonly double[] Delays = { -0.8, -0.7, -0.6, -0.5, -0.4, -0.3, -0.2, -0.1, 0, 0.1 };
    private readonly ScaleTransform?[] _scales = new ScaleTransform?[10];
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsWaveBarsLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsWaveBarsLoader),
            new FrameworkPropertyMetadata(typeof(CheemsWaveBarsLoader)));
    }

    public CheemsWaveBarsLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var index = 0; index < 10; index++)
        {
            if (GetTemplateChild($"PartBar{index + 1}") is not FrameworkElement bar) continue;
            var scale = new ScaleTransform(1, 1);
            bar.RenderTransform = scale;
            bar.RenderTransformOrigin = new Point(0.5, 0.5);
            _scales[index] = scale;
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
        for (var index = 0; index < 10; index++)
        {
            var scale = _scales[index];
            if (scale is null) continue;

            // CSS negative delay means动画从已运行 |delay| 秒的位置开始。
            var active = seconds - Delays[index];
            if (active < 0)
            {
                scale.ScaleY = 1;
                continue;
            }

            var phase = active / Duration;
            phase -= Math.Floor(phase);
            scale.ScaleY = phase switch
            {
                <= 0.20 => 1 + 1.32 * EaseInOut(phase / 0.20),
                <= 0.40 => 2.32 - 1.32 * EaseInOut((phase - 0.20) / 0.20),
                _ => 1
            };
        }
    }

    private static double EaseInOut(double x) => CubicBezier(x, 0.42, 0, 0.58, 1);

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
