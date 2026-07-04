using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CheemsControl;

/// <summary>
/// Uiverse milley69 polyline loading animation 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartBackName, Type = typeof(Path))]
[TemplatePart(Name = PartFrontName, Type = typeof(Path))]
public sealed class CheemsPolylineLoader : Control
{
    private const string PartBackName = "PartBack";
    private const string PartFrontName = "PartFront";
    private const double DurationSeconds = 1.4;
    private const double InitialStrokeDashOffset = 64;
    private const double FadeOutKeyframe = 0.725;

    private Path? _frontPath;
    private double _animationStart;
    private bool _renderingSubscribed;

    static CheemsPolylineLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsPolylineLoader),
            new FrameworkPropertyMetadata(typeof(CheemsPolylineLoader)));
    }

    public CheemsPolylineLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _frontPath = GetTemplateChild(PartFrontName) as Path;
        ApplyFrame(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animationStart = Stopwatch.GetTimestamp();
        ApplyFrame(0);

        if (SystemParameters.ClientAreaAnimation)
        {
            SubscribeRendering();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeRendering();
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        var seconds = (Stopwatch.GetTimestamp() - _animationStart) / (double)Stopwatch.Frequency;
        var phase = seconds / DurationSeconds;
        phase -= Math.Floor(phase);
        ApplyFrame(phase);
    }

    private void ApplyFrame(double phase)
    {
        if (_frontPath is null)
        {
            return;
        }

        // WPF 的 StrokeDashOffset 以 StrokeThickness 为单位。
        // SVG 的 192px / 3px = 64，视觉位移仍严格对应 192px → 0px。
        var dashOffset = InitialStrokeDashOffset * (1 - phase);

        // CSS 只在 72.5% 声明 opacity: 0；缺失的 0% 和 100% 关键帧
        // 使用元素基础值 1。因此透明度先 1 → 0，再由 0 → 1。
        double opacity;
        if (phase <= FadeOutKeyframe)
        {
            opacity = 1 - (phase / FadeOutKeyframe);
        }
        else
        {
            opacity = (phase - FadeOutKeyframe) / (1 - FadeOutKeyframe);
        }

        // 应用到 Path 的 StrokeDashOffset 和 Opacity
        _frontPath.StrokeDashOffset = dashOffset;
        _frontPath.Opacity = opacity;
    }
}
