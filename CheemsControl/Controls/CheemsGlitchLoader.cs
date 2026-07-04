using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>Uiverse andrew-demchenk0 Glitch Loader 的 WPF 等价实现。</summary>
[TemplatePart(Name = "PartGlitch", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartBefore", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartAfter", Type = typeof(FrameworkElement))]
public sealed class CheemsGlitchLoader : Control
{
    private const double ShiftDuration = 1.0;
    private const double GlitchDuration = 0.4;

    private static readonly VectorKeyFrame[] ShiftFrames =
    {
        new(0, 0, 0), new(0.40, 0, 0), new(0.41, 10, 0), new(0.42, -10, 0),
        new(0.44, 0, 0), new(0.58, 0, 0), new(0.59, 40, 10), new(0.60, -40, -10),
        new(0.61, 0, 0), new(0.63, 10, -5), new(0.65, 0, 0), new(0.69, 0, 0),
        new(0.70, -50, -20), new(0.71, 10, -10), new(0.73, 0, 0), new(1, 0, 0)
    };

    private static readonly VectorKeyFrame[] GlitchFrames =
    {
        new(0, 0, 0), new(0.20, -3, 3), new(0.40, -3, -3),
        new(0.60, 3, 3), new(0.80, 3, -3), new(1, 0, 0)
    };

    private MatrixTransform? _glitchTransform;
    private TranslateTransform? _beforeTransform;
    private TranslateTransform? _afterTransform;
    private long _startedAt;
    private bool _renderingSubscribed;

    static CheemsGlitchLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsGlitchLoader),
            new FrameworkPropertyMetadata(typeof(CheemsGlitchLoader)));
    }

    public CheemsGlitchLoader()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PartGlitch") is FrameworkElement glitch)
        {
            _glitchTransform = new MatrixTransform(Matrix.Identity);
            glitch.RenderTransform = _glitchTransform;
            glitch.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            _glitchTransform = null;
        }

        _beforeTransform = InstallTranslation("PartBefore");
        _afterTransform = InstallTranslation("PartAfter");
        ApplyFrame(0);
    }

    private TranslateTransform? InstallTranslation(string partName)
    {
        if (GetTemplateChild(partName) is not FrameworkElement element) return null;
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startedAt = Stopwatch.GetTimestamp();
        ApplyFrame(0);
        if (!SystemParameters.ClientAreaAnimation || _renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
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
        var shiftPhase = AlternatingPhase(seconds, ShiftDuration);
        var skew = Evaluate(ShiftFrames, shiftPhase, 0.42, 0, 0.58, 1);
        if (_glitchTransform is not null)
        {
            var tangentX = Math.Tan(skew.X * Math.PI / 180);
            var tangentY = Math.Tan(skew.Y * Math.PI / 180);
            // CSS skewX(x) skewY(y)：列向量矩阵相乘后转换为 WPF Matrix 的行向量字段。
            _glitchTransform.Matrix = new Matrix(
                1 + tangentX * tangentY,
                tangentY,
                tangentX,
                1,
                0,
                0);
        }

        var phase = LoopPhase(seconds, GlitchDuration);
        var before = Evaluate(GlitchFrames, phase, 0.25, 0.46, 0.45, 0.94);
        var after = Evaluate(GlitchFrames, 1 - phase, 0.25, 0.46, 0.45, 0.94);
        SetTranslation(_beforeTransform, before);
        SetTranslation(_afterTransform, after);
    }

    private static void SetTranslation(TranslateTransform? transform, Point point)
    {
        if (transform is null) return;
        transform.X = point.X;
        transform.Y = point.Y;
    }

    private static double LoopPhase(double seconds, double duration)
    {
        var value = seconds / duration;
        return value - Math.Floor(value);
    }

    private static double AlternatingPhase(double seconds, double duration)
    {
        var value = seconds / duration;
        var iteration = (long)Math.Floor(value);
        var phase = value - iteration;
        return iteration % 2 == 0 ? phase : 1 - phase;
    }

    private static Point Evaluate(
        IReadOnlyList<VectorKeyFrame> frames,
        double phase,
        double x1,
        double y1,
        double x2,
        double y2)
    {
        for (var index = 1; index < frames.Count; index++)
        {
            var right = frames[index];
            if (phase > right.Time) continue;
            var left = frames[index - 1];
            var progress = (phase - left.Time) / (right.Time - left.Time);
            progress = CubicBezier(progress, x1, y1, x2, y2);
            return new Point(
                left.X + (right.X - left.X) * progress,
                left.Y + (right.Y - left.Y) * progress);
        }

        return new Point(frames[^1].X, frames[^1].Y);
    }

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

        var low = 0.0;
        var high = 1.0;
        for (var index = 0; index < 12; index++)
        {
            var sampled = Sample(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001) break;
            if (sampled < x) low = parameter; else high = parameter;
            parameter = (low + high) / 2;
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

    private readonly record struct VectorKeyFrame(double Time, double X, double Y);
}
