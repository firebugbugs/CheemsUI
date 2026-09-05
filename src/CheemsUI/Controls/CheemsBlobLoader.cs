using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse vikramsinghnegi Blob Loader 的 WPF 等价实现。</summary>
public sealed class CheemsBlobLoader : Control
{
    private const double Duration = 2.0;
    private readonly TranslateTransform?[] _translations = new TranslateTransform?[4];
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsBlobLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsBlobLoader),
            new FrameworkPropertyMetadata(typeof(CheemsBlobLoader)));
    }

    public CheemsBlobLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var index = 0; index < 4; index++)
        {
            if (GetTemplateChild($"PartBlob{index + 1}") is not FrameworkElement blob) continue;
            var translation = new TranslateTransform();
            blob.RenderTransform = translation;
            _translations[index] = translation;
        }
        ApplyFrame(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startedAt = Stopwatch.GetTimestamp();
        ApplyFrame(0);
        if (!_renderingSubscribed)
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
        ApplyPair(0, seconds / Duration);
        ApplyPair(1, seconds / Duration + 0.5); // animation-delay: -1s / 2s
    }

    private void ApplyPair(int pairIndex, double rawPhase)
    {
        var phase = rawPhase - Math.Floor(rawPhase);
        var (x, y) = phase switch
        {
            <= 0.25 => (44.8 * Ease(phase / 0.25), 0),
            <= 0.50 => (44.8, 44.8 * Ease((phase - 0.25) / 0.25)),
            <= 0.75 => (44.8 * (1 - Ease((phase - 0.50) / 0.25)), 44.8),
            _ => (0, 44.8 * (1 - Ease((phase - 0.75) / 0.25)))
        };

        // 两个同步图层共同近似 CSS blur + contrast 的阈值收边效果。
        SetTranslation(_translations[pairIndex], x, y);
        SetTranslation(_translations[pairIndex + 2], x, y);
    }

    private static void SetTranslation(TranslateTransform? transform, double x, double y)
    {
        if (transform is null) return;
        transform.X = x;
        transform.Y = y;
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
