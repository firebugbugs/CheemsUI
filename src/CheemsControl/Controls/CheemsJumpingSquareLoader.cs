using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse alexruix 跳跃方块 Loader 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartBodyName, Type = typeof(Border))]
[TemplatePart(Name = PartShadowName, Type = typeof(FrameworkElement))]
public sealed class CheemsJumpingSquareLoader : Control
{
    private const string PartBodyName = "PartBody";
    private const string PartShadowName = "PartShadow";
    private const double Duration = 0.5;

    private Border? _body;
    private ScaleTransform? _bodyScale;
    private RotateTransform? _bodyRotation;
    private TranslateTransform? _bodyTranslation;
    private ScaleTransform? _shadowScale;
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsJumpingSquareLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsJumpingSquareLoader),
            new FrameworkPropertyMetadata(typeof(CheemsJumpingSquareLoader)));
    }

    public CheemsJumpingSquareLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _body = GetTemplateChild(PartBodyName) as Border;
        if (_body is not null)
        {
            _bodyScale = new ScaleTransform(1, 1);
            _bodyRotation = new RotateTransform();
            _bodyTranslation = new TranslateTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(_bodyScale);
            transforms.Children.Add(_bodyRotation);
            transforms.Children.Add(_bodyTranslation);
            _body.RenderTransform = transforms;
            _body.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        if (GetTemplateChild(PartShadowName) is FrameworkElement shadow)
        {
            _shadowScale = new ScaleTransform(1, 1);
            shadow.RenderTransform = _shadowScale;
            shadow.RenderTransformOrigin = new Point(0.5, 0.5);
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
        var phase = seconds / Duration;
        phase -= Math.Floor(phase);

        if (_body is not null && _bodyScale is not null &&
            _bodyRotation is not null && _bodyTranslation is not null)
        {
            _bodyTranslation.Y = Interpolate(phase,
                (0.00, 0), (0.15, 0), (0.25, 9), (0.50, 18), (0.75, 9), (1.00, 0));
            _bodyRotation.Angle = Interpolate(phase,
                (0.00, 0), (0.15, 0), (0.25, 22.5), (0.50, 45), (0.75, 67.5), (1.00, 90));
            _bodyScale.ScaleX = 1;
            _bodyScale.ScaleY = Interpolate(phase,
                (0.00, 1), (0.25, 1), (0.50, 0.9), (0.75, 1), (1.00, 1));

            var bottomRightRadius = Interpolate(phase,
                (0.00, 4), (0.15, 3), (0.25, 4), (0.50, 40), (0.75, 4), (1.00, 4));
            _body.CornerRadius = new CornerRadius(4, 4, bottomRightRadius, 4);
        }

        if (_shadowScale is not null)
        {
            _shadowScale.ScaleX = Interpolate(phase, (0.00, 1), (0.50, 1.2), (1.00, 1));
        }
    }

    private static double Interpolate(double phase, params (double Time, double Value)[] frames)
    {
        for (var index = 1; index < frames.Length; index++)
        {
            if (phase > frames[index].Time) continue;
            var left = frames[index - 1];
            var right = frames[index];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            return left.Value + (right.Value - left.Value) * progress;
        }

        return frames[^1].Value;
    }
}
