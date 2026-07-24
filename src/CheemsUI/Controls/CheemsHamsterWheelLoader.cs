using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Nawsome 仓鼠跑轮 Loader 的 WPF 等价实现。</summary>
[TemplatePart(Name = "PartHamster", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartBody", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartHead", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartEar", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartEye", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartFrontRight", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartFrontLeft", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartBackRight", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartBackLeft", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartTail", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartSpoke", Type = typeof(FrameworkElement))]
public sealed class CheemsHamsterWheelLoader : Control
{
    private const double DurationSeconds = 1.0;
    private readonly Dictionary<string, RotateTransform> _rotations = new();
    private ScaleTransform? _eyeScale;
    private long _startedAt;
    private bool _renderingSubscribed;

    private static readonly KeyFrame[] HamsterFrames =
    {
        new(0, 4), new(0.5, 0), new(1, 4)
    };
    private static readonly KeyFrame[] HeadFrames = Alternating(0, 8);
    private static readonly KeyFrame[] EarFrames = Alternating(0, 12);
    private static readonly KeyFrame[] BodyFrames = Alternating(0, -2);
    private static readonly KeyFrame[] FrontRightFrames = Alternating(50, -30);
    private static readonly KeyFrame[] FrontLeftFrames = Alternating(-30, 50);
    private static readonly KeyFrame[] BackRightFrames = Alternating(-60, 20);
    private static readonly KeyFrame[] BackLeftFrames = Alternating(20, -60);
    private static readonly KeyFrame[] TailFrames = Alternating(30, 10);
    private static readonly KeyFrame[] EyeFrames =
    {
        new(0, 1), new(0.9, 1), new(0.95, 0), new(1, 1)
    };

    static CheemsHamsterWheelLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsHamsterWheelLoader),
            new FrameworkPropertyMetadata(typeof(CheemsHamsterWheelLoader)));
    }

    public CheemsHamsterWheelLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rotations.Clear();

        InstallRotation("PartHamster", 0.5, 0);
        InstallRotation("PartBody", 0.17, 0.5);
        InstallRotation("PartHead", 1, 0.5);
        InstallRotation("PartEar", 0.5, 0.75);
        InstallRotation("PartFrontRight", 0.5, 0);
        InstallRotation("PartFrontLeft", 0.5, 0);
        InstallRotation("PartBackRight", 0.5, 0.3);
        InstallRotation("PartBackLeft", 0.5, 0.3);
        InstallRotation("PartTail", 0.25, 0.5);
        InstallRotation("PartSpoke", 0.5, 0.5);

        if (GetTemplateChild("PartEye") is FrameworkElement eye)
        {
            _eyeScale = new ScaleTransform(1, 1);
            eye.RenderTransform = _eyeScale;
            eye.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        // CSS rotate(...) translate(...) 按矩阵从右向左作用：先位移，再绕 50% 0 旋转。
        if (GetTemplateChild("PartHamster") is FrameworkElement hamster &&
            _rotations.TryGetValue("PartHamster", out var hamsterRotation))
        {
            var group = new TransformGroup();
            group.Children.Add(new TranslateTransform(-11.2, 25.9));
            group.Children.Add(hamsterRotation);
            hamster.RenderTransform = group;
        }

        ApplyFrame(0);
    }

    private void InstallRotation(string partName, double originX, double originY)
    {
        if (GetTemplateChild(partName) is not FrameworkElement element)
        {
            return;
        }

        var rotation = new RotateTransform();
        element.RenderTransform = rotation;
        element.RenderTransformOrigin = new Point(originX, originY);
        _rotations[partName] = rotation;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startedAt = Stopwatch.GetTimestamp();
        ApplyFrame(0);
        if (SystemParameters.ClientAreaAnimation) SubscribeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void SubscribeRendering()
    {
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsVisible) return;
        var seconds = (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
        ApplyFrame(seconds / DurationSeconds - Math.Floor(seconds / DurationSeconds));
    }

    private void ApplyFrame(double phase)
    {
        SetRotation("PartHamster", Evaluate(HamsterFrames, phase, true));
        SetRotation("PartHead", Evaluate(HeadFrames, phase, true));
        SetRotation("PartEar", Evaluate(EarFrames, phase, true));
        SetRotation("PartBody", Evaluate(BodyFrames, phase, true));
        SetRotation("PartFrontRight", Evaluate(FrontRightFrames, phase, false));
        SetRotation("PartFrontLeft", Evaluate(FrontLeftFrames, phase, false));
        SetRotation("PartBackRight", Evaluate(BackRightFrames, phase, false));
        SetRotation("PartBackLeft", Evaluate(BackLeftFrames, phase, false));
        SetRotation("PartTail", Evaluate(TailFrames, phase, false));
        SetRotation("PartSpoke", -360 * phase);
        if (_eyeScale is not null) _eyeScale.ScaleY = Evaluate(EyeFrames, phase, false);
    }

    private void SetRotation(string name, double angle)
    {
        if (_rotations.TryGetValue(name, out var rotation)) rotation.Angle = angle;
    }

    private static double Evaluate(IReadOnlyList<KeyFrame> frames, double phase, bool easeInOut)
    {
        for (var index = 1; index < frames.Count; index++)
        {
            var right = frames[index];
            if (phase > right.Time) continue;
            var left = frames[index - 1];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            if (easeInOut) progress = CubicBezier(progress, 0.42, 0, 0.58, 1);
            return left.Value + (right.Value - left.Value) * progress;
        }
        return frames[^1].Value;
    }

    private static KeyFrame[] Alternating(double resting, double active) =>
        new KeyFrame[]
    {
        new(0, resting), new(0.125, active), new(0.25, resting), new(0.375, active),
        new(0.5, resting), new(0.625, active), new(0.75, resting), new(0.875, active), new(1, resting)
    };

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        var parameter = Math.Clamp(x, 0, 1);
        for (var index = 0; index < 8; index++)
        {
            var error = Sample(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            parameter = Math.Clamp(parameter - error / derivative, 0, 1);
        }
        var low = 0.0; var high = 1.0;
        for (var index = 0; index < 12; index++)
        {
            var sampled = Sample(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001) break;
            if (sampled < x) low = parameter; else high = parameter;
            parameter = (low + high) / 2;
        }
        return Sample(parameter, y1, y2);
    }

    private static double Sample(double t, double a, double b)
    {
        var i = 1 - t;
        return 3 * i * i * t * a + 3 * i * t * t * b + t * t * t;
    }

    private static double SampleDerivative(double t, double a, double b)
    {
        var i = 1 - t;
        return 3 * i * i * a + 6 * i * t * (b - a) + 3 * t * t * (1 - b);
    }

    private readonly record struct KeyFrame(double Time, double Value);
}
