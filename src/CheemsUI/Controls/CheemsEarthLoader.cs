using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Novaxlo Earth Loader 的 WPF 等价实现。</summary>
public sealed class CheemsEarthLoader : Control
{
    private const double LoopDuration = 5.0;
    private const double DelayedStart = 0.75;
    private readonly FrameworkElement?[] _lands = new FrameworkElement?[4];
    private readonly SkewTransform?[] _skews = new SkewTransform?[4];
    private readonly RotateTransform?[] _rotations = new RotateTransform?[4];
    private FrameworkElement? _startupFlash;
    private FrameworkElement? _insetShade;
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsEarthLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsEarthLoader),
            new FrameworkPropertyMetadata(typeof(CheemsEarthLoader)));
    }

    public CheemsEarthLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var index = 0; index < 4; index++)
        {
            var land = GetTemplateChild($"PartLand{index + 1}") as FrameworkElement;
            _lands[index] = land;
            if (land is null) continue;
            var rotation = new RotateTransform();
            var skew = new SkewTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(rotation);
            transforms.Children.Add(skew);
            land.RenderTransform = transforms;
            land.RenderTransformOrigin = new Point(0.5, 0.5);
            _rotations[index] = rotation;
            _skews[index] = skew;
        }
        _startupFlash = GetTemplateChild("PartStartupFlash") as FrameworkElement;
        _insetShade = GetTemplateChild("PartInsetShade") as FrameworkElement;
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
        ApplyLand(0, seconds, true, true);
        ApplyLand(1, seconds, true, false);
        ApplyLand(2, seconds, false, false);
        ApplyLand(3, seconds, false, true);

        var startupPhase = Math.Clamp(seconds, 0, 1);
        var flashOpacity = startupPhase <= 0.75 ? 0.82 : 0.82 * (1 - (startupPhase - 0.75) / 0.25);
        if (_startupFlash is not null) _startupFlash.Opacity = flashOpacity;
        if (_insetShade is not null) _insetShade.Opacity = startupPhase <= 0.75 ? 0 : (startupPhase - 0.75) / 0.25;
    }

    private void ApplyLand(int index, double seconds, bool roundOne, bool delayed)
    {
        var land = _lands[index];
        if (land is null) return;
        if (delayed && seconds < DelayedStart)
        {
            Canvas.SetLeft(land, 0);
            land.Opacity = 1;
            _skews[index]!.AngleX = 0;
            _rotations[index]!.Angle = 0;
            return;
        }

        var activeSeconds = seconds - (delayed ? DelayedStart : 0);
        var phase = activeSeconds / LoopDuration;
        phase -= Math.Floor(phase);
        var frames = roundOne ? RoundOneFrames : RoundTwoFrames;
        Canvas.SetLeft(land, Evaluate(frames, phase, frame => frame.Left));
        land.Opacity = Evaluate(frames, phase, frame => frame.Opacity);
        _skews[index]!.AngleX = Evaluate(frames, phase, frame => frame.Skew);
        _rotations[index]!.Angle = Evaluate(frames, phase, frame => frame.Rotation);
    }

    private static double Evaluate(Frame[] frames, double phase, Func<Frame, double> selector)
    {
        for (var index = 1; index < frames.Length; index++)
        {
            if (phase > frames[index].Time) continue;
            var left = frames[index - 1];
            var right = frames[index];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            return selector(left) + (selector(right) - selector(left)) * progress;
        }
        return selector(frames[^1]);
    }

    private static readonly Frame[] RoundOneFrames =
    {
        new(0.00, -32, 1, 0, 0), new(0.30, -96, 1, -25, 25),
        new(0.31, -96, 0, -25, 25), new(0.35, 112, 0, 25, -25),
        new(0.45, 112, 1, 25, -25), new(1.00, -32, 1, 0, 0)
    };

    private static readonly Frame[] RoundTwoFrames =
    {
        new(0.00, 80, 1, 0, 0), new(0.75, -112, 1, -25, 25),
        new(0.76, -112, 0, -25, 25), new(0.77, 128, 0, 25, -25),
        new(0.80, 128, 1, 25, -25), new(1.00, 80, 1, 0, 0)
    };

    private readonly record struct Frame(double Time, double Left, double Opacity, double Skew, double Rotation);
}
