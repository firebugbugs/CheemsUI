using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse PriyanshuGupta28 AI Matrix Loader 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartGlowName, Type = typeof(FrameworkElement))]
public sealed class CheemsAiMatrixLoader : Control
{
    private const string PartGlowName = "PartGlow";
    private const double FallDuration = 2.0;
    private const double FlickerDuration = 0.5;
    private const double PulseDuration = 2.0;

    private static readonly double[] Delays = { 0.1, 0.3, 0.5, 0.7, 0.9, 1.1, 1.3, 1.5 };
    private static readonly KeyFrame[] FallTranslationFrames =
    {
        new(0.00, -50), new(0.20, 0), new(0.80, 0), new(1.00, 50)
    };
    private static readonly KeyFrame[] FallRotationFrames =
    {
        new(0.00, 90), new(0.20, 0), new(0.80, 0), new(1.00, -90)
    };
    private static readonly KeyFrame[] FlickerFrames =
    {
        new(0.00, 0.8), new(0.19, 0.8), new(0.20, 0.2), new(0.21, 0.8), new(1.00, 0.8)
    };
    private static readonly KeyFrame[] PulseFrames =
    {
        new(0.00, 0.3), new(0.50, 0.7), new(1.00, 0.3)
    };

    private readonly FrameworkElement?[] _digits = new FrameworkElement?[8];
    private readonly ScaleTransform?[] _digitScales = new ScaleTransform?[8];
    private readonly TranslateTransform?[] _digitTranslations = new TranslateTransform?[8];
    private FrameworkElement? _glow;
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsAiMatrixLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsAiMatrixLoader),
            new FrameworkPropertyMetadata(typeof(CheemsAiMatrixLoader)));
    }

    public CheemsAiMatrixLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        for (var index = 0; index < _digits.Length; index++)
        {
            var digit = GetTemplateChild($"PartDigit{index + 1}") as FrameworkElement;
            _digits[index] = digit;
            if (digit is null)
            {
                _digitScales[index] = null;
                _digitTranslations[index] = null;
                continue;
            }

            var scale = new ScaleTransform(1, 1);
            var translation = new TranslateTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(translation);
            digit.RenderTransform = transforms;
            digit.RenderTransformOrigin = new Point(0.5, 0.5);
            _digitScales[index] = scale;
            _digitTranslations[index] = translation;
        }

        _glow = GetTemplateChild(PartGlowName) as FrameworkElement;
        ApplyFrame(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startedAt = Stopwatch.GetTimestamp();
        ApplyFrame(0);
        if (SystemParameters.ClientAreaAnimation)
        {
            SubscribeRendering();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

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

        var seconds = (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
        ApplyFrame(seconds);
    }

    private void ApplyFrame(double seconds)
    {
        for (var index = 0; index < _digits.Length; index++)
        {
            var digit = _digits[index];
            if (digit is null)
            {
                continue;
            }

            var activeTime = seconds - Delays[index];
            if (activeTime < 0)
            {
                digit.Opacity = 0;
                _digitScales[index]!.ScaleY = 1;
                _digitTranslations[index]!.Y = 0;
                continue;
            }

            var fallPhase = PositivePhase(activeTime / FallDuration);
            var flickerPhase = PositivePhase(activeTime / FlickerDuration);
            var angle = Evaluate(FallRotationFrames, fallPhase);

            // rotateX 的正交投影在二维平面上的纵向比例为 cos(angle)。
            _digitScales[index]!.ScaleY = Math.Cos(angle * Math.PI / 180.0);
            _digitTranslations[index]!.Y = Evaluate(FallTranslationFrames, fallPhase);

            // 两个 CSS animation 同时写 opacity；后声明的 matrix-flicker 覆盖 matrix-fall。
            digit.Opacity = Evaluate(FlickerFrames, flickerPhase);
        }

        if (_glow is not null)
        {
            _glow.Opacity = Evaluate(PulseFrames, PositivePhase(seconds / PulseDuration));
        }
    }

    private static double Evaluate(IReadOnlyList<KeyFrame> frames, double phase)
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
            progress = CubicBezier(progress, 0.25, 0.1, 0.25, 1.0); // CSS 默认 ease
            return left.Value + (right.Value - left.Value) * progress;
        }

        return frames[^1].Value;
    }

    private static double PositivePhase(double value) => value - Math.Floor(value);

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        var parameter = x;
        for (var index = 0; index < 8; index++)
        {
            var error = Sample(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            parameter = Math.Clamp(parameter - error / derivative, 0.0, 1.0);
        }

        var low = 0.0;
        var high = 1.0;
        for (var index = 0; index < 12; index++)
        {
            var sampled = Sample(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001) break;
            if (sampled < x) low = parameter; else high = parameter;
            parameter = (low + high) * 0.5;
        }

        return Sample(parameter, y1, y2);
    }

    private static double Sample(double time, double first, double second)
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

    private readonly record struct KeyFrame(double Time, double Value);
}
