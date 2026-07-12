using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse Nawsome 打字机加载动画的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartRootName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartSlideName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPaperName, Type = typeof(FrameworkElement))]
public sealed class CheemsTypewriterLoader : Control
{
    private const string PartRootName = "PartRoot";
    private const string PartSlideName = "PartSlide";
    private const string PartPaperName = "PartPaper";
    private const double DurationSeconds = 3.0;

    private static readonly ScalarKeyFrame[] BounceFrames =
    {
        new(0.00, 0),
        new(0.85, 0),
        new(0.89, -4),
        new(0.92, 0),
        new(0.95, 2),
        new(1.00, 0),
    };

    private static readonly ScalarKeyFrame[] SlideFrames =
    {
        new(0.00, 14),
        new(0.05, 14),
        new(0.15, 6),
        new(0.30, 6),
        new(0.40, 0),
        new(0.55, 0),
        new(0.65, -4),
        new(0.70, -4),
        new(0.80, -12),
        new(0.89, -12),
        new(1.00, 14),
    };

    private static readonly ScalarKeyFrame[] PaperFrames =
    {
        new(0.00, 46),
        new(0.05, 46),
        new(0.20, 34),
        new(0.30, 34),
        new(0.40, 22),
        new(0.55, 22),
        new(0.65, 10),
        new(0.70, 10),
        new(0.80, 0),
        new(0.85, 0),
        new(0.92, 46),
        new(1.00, 46),
    };

    // keyboard05 的每一个完整 box-shadow 关键帧。
    private static readonly double[] KeyboardTimes =
    {
        0.00, 0.05, 0.09, 0.12, 0.18, 0.21, 0.27, 0.30, 0.36, 0.39,
        0.45, 0.48, 0.54, 0.57, 0.63, 0.66, 0.72, 0.75, 0.81, 0.84, 1.00,
    };

    // 位 0..5 是第一行，位 6..11 是第二行；按下量均为 2px。
    private static readonly int[] KeyboardPressedMasks =
    {
        0, 0, 1 << 0, 0, 1 << 3, 0, 1 << 6, 0, (1 << 8) | (1 << 9) | (1 << 10), 0,
        1 << 5, 0, 1 << 1, 0, 1 << 11, 0, 1 << 2, 0, 1 << 7, 0, 0,
    };

    private readonly TranslateTransform[] _keyTransforms = new TranslateTransform[12];

    private TranslateTransform? _rootTransform;
    private TranslateTransform? _slideTransform;
    private TranslateTransform? _paperTransform;
    private long _animationStart;
    private bool _renderingSubscribed;

    static CheemsTypewriterLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsTypewriterLoader),
            new FrameworkPropertyMetadata(typeof(CheemsTypewriterLoader)));
    }

    public CheemsTypewriterLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootTransform = InstallTranslateTransform(GetTemplateChild(PartRootName) as FrameworkElement);
        _slideTransform = InstallTranslateTransform(GetTemplateChild(PartSlideName) as FrameworkElement);
        _paperTransform = InstallTranslateTransform(GetTemplateChild(PartPaperName) as FrameworkElement);

        for (var index = 0; index < _keyTransforms.Length; index++)
        {
            _keyTransforms[index] = InstallTranslateTransform(
                GetTemplateChild($"PartKey{index + 1}") as FrameworkElement) ?? new TranslateTransform();
        }

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
        if (_rootTransform is not null)
        {
            _rootTransform.Y = Evaluate(BounceFrames, phase, useCssEase: false);
        }

        if (_slideTransform is not null)
        {
            _slideTransform.X = Evaluate(SlideFrames, phase, useCssEase: true);
        }

        if (_paperTransform is not null)
        {
            _paperTransform.Y = Evaluate(PaperFrames, phase, useCssEase: false);
        }

        for (var index = 0; index < _keyTransforms.Length; index++)
        {
            _keyTransforms[index].Y = EvaluateKeyboardOffset(index, phase);
        }
    }

    private static TranslateTransform? InstallTranslateTransform(FrameworkElement? element)
    {
        if (element is null)
        {
            return null;
        }

        // 每个实例独享可写 Transform，避免模板 Freezable 被冻结或跨实例共享。
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private static double Evaluate(
        IReadOnlyList<ScalarKeyFrame> frames,
        double phase,
        bool useCssEase)
    {
        for (var index = 1; index < frames.Count; index++)
        {
            var right = frames[index];
            if (phase > right.Time)
            {
                continue;
            }

            var left = frames[index - 1];
            var span = right.Time - left.Time;
            var progress = span <= 0 ? 1 : (phase - left.Time) / span;
            if (useCssEase)
            {
                progress = CubicBezier(progress, 0.25, 0.1, 0.25, 1.0);
            }

            return Lerp(left.Value, right.Value, progress);
        }

        return frames[^1].Value;
    }

    private static double EvaluateKeyboardOffset(int keyIndex, double phase)
    {
        var bit = 1 << keyIndex;

        for (var index = 1; index < KeyboardTimes.Length; index++)
        {
            if (phase > KeyboardTimes[index])
            {
                continue;
            }

            var startTime = KeyboardTimes[index - 1];
            var endTime = KeyboardTimes[index];
            var progress = (phase - startTime) / (endTime - startTime);
            var from = (KeyboardPressedMasks[index - 1] & bit) == 0 ? 0.0 : 2.0;
            var to = (KeyboardPressedMasks[index] & bit) == 0 ? 0.0 : 2.0;
            return Lerp(from, to, progress);
        }

        return 0;
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

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;

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
