using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse Shoh2008 洗衣机加载动画的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartMachineName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartDoorName, Type = typeof(FrameworkElement))]
public sealed class CheemsWashingMachineLoader : Control
{
    private const string PartMachineName = "PartMachine";
    private const string PartDoorName = "PartDoor";
    private const double DurationSeconds = 3.0;

    // 50% 在源码中重复声明；同一关键帧后声明的 rotate(0) 覆盖 rotate(-.5deg)。
    private static readonly ScalarKeyFrame[] ShakeFrames =
    {
        new(0.00, 0),
        new(0.50, 0),
        new(0.65, 0.5),
        new(0.75, -0.5),
        new(0.80, 0.5),
        new(0.84, -0.5),
        new(0.88, 0.5),
        new(0.92, -0.5),
        new(0.96, 0.5),
        new(1.00, 0),
    };

    private static readonly ScalarKeyFrame[] SpinFrames =
    {
        new(0.00, 0),
        new(0.50, 360),
        new(0.75, 750),
        new(1.00, 1800),
    };

    private RotateTransform? _machineRotation;
    private RotateTransform? _doorRotation;
    private long _animationStart;
    private bool _renderingSubscribed;

    static CheemsWashingMachineLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsWashingMachineLoader),
            new FrameworkPropertyMetadata(typeof(CheemsWashingMachineLoader)));
    }

    public CheemsWashingMachineLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _machineRotation = InstallRotation(
            GetTemplateChild(PartMachineName) as FrameworkElement,
            centerX: 60,
            centerY: 180);
        _doorRotation = InstallRotation(
            GetTemplateChild(PartDoorName) as FrameworkElement,
            centerX: 47.5,
            centerY: 47.5);
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
        if (_machineRotation is not null)
        {
            _machineRotation.Angle = Evaluate(ShakeFrames, phase);
        }

        if (_doorRotation is not null)
        {
            _doorRotation.Angle = Evaluate(SpinFrames, phase);
        }
    }

    private static RotateTransform? InstallRotation(
        FrameworkElement? element,
        double centerX,
        double centerY)
    {
        if (element is null)
        {
            return null;
        }

        var transform = new RotateTransform(0, centerX, centerY);
        element.RenderTransform = transform;
        return transform;
    }

    private static double Evaluate(IReadOnlyList<ScalarKeyFrame> frames, double phase)
    {
        for (var index = 1; index < frames.Count; index++)
        {
            var right = frames[index];
            if (phase > right.Time)
            {
                continue;
            }

            var left = frames[index - 1];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            // CSS ease-in-out = cubic-bezier(.42,0,.58,1)，逐关键帧区间生效。
            progress = CubicBezier(progress, 0.42, 0, 0.58, 1);
            return left.Value + (right.Value - left.Value) * progress;
        }

        return frames[^1].Value;
    }

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        x = Math.Max(0, Math.Min(1, x));
        var parameter = x;

        for (var index = 0; index < 8; index++)
        {
            var error = SampleCurve(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001)
            {
                break;
            }

            parameter -= error / derivative;
            parameter = Math.Max(0, Math.Min(1, parameter));
        }

        var low = 0.0;
        var high = 1.0;
        for (var index = 0; index < 12; index++)
        {
            var sampled = SampleCurve(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001)
            {
                break;
            }

            if (sampled < x) low = parameter;
            else high = parameter;
            parameter = (low + high) * 0.5;
        }

        return SampleCurve(parameter, y1, y2);
    }

    private static double SampleCurve(double time, double first, double second)
    {
        var inverse = 1 - time;
        return 3 * inverse * inverse * time * first
             + 3 * inverse * time * time * second
             + time * time * time;
    }

    private static double SampleDerivative(double time, double first, double second)
    {
        var inverse = 1 - time;
        return 3 * inverse * inverse * first
             + 6 * inverse * time * (second - first)
             + 3 * time * time * (1 - second);
    }

    private readonly struct ScalarKeyFrame
    {
        public ScalarKeyFrame(double time, double value)
        {
            Time = time;
            Value = value;
        }

        public double Time { get; }
        public double Value { get; }
    }
}
