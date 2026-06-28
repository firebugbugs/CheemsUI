using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CheemsControl;

/// <summary>
/// Uiverse Nawsome 三维红色摇臂开关的 WPF 等价实现。
/// </summary>
public sealed class CheemsRockerSwitch : ToggleButton
{
    private const double StateTransitionSeconds = 0.3;
    private const double FlickerDelaySeconds = 0.3;
    private const double FlickerDurationSeconds = 0.2;
    private const double LightOffDurationSeconds = 1.0;
    private const double LightOffFadeEnd = 0.8;

    private static readonly DependencyPropertyKey RockerAnglePropertyKey = RegisterReadOnlyDouble(nameof(RockerAngle), -25.0);
    private static readonly DependencyPropertyKey ShineOpacityPropertyKey = RegisterReadOnlyDouble(nameof(ShineOpacity), 0.3);
    private static readonly DependencyPropertyKey ShadowOpacityPropertyKey = RegisterReadOnlyDouble(nameof(ShadowOpacity), 1.0);
    private static readonly DependencyPropertyKey GlowOpacityPropertyKey = RegisterReadOnlyDouble(nameof(GlowOpacity), 0.0);
    private static readonly DependencyPropertyKey LightOpacityPropertyKey = RegisterReadOnlyDouble(nameof(LightOpacity), 0.0);

    public static readonly DependencyProperty RockerAngleProperty = RockerAnglePropertyKey.DependencyProperty;
    public static readonly DependencyProperty ShineOpacityProperty = ShineOpacityPropertyKey.DependencyProperty;
    public static readonly DependencyProperty ShadowOpacityProperty = ShadowOpacityPropertyKey.DependencyProperty;
    public static readonly DependencyProperty GlowOpacityProperty = GlowOpacityPropertyKey.DependencyProperty;
    public static readonly DependencyProperty LightOpacityProperty = LightOpacityPropertyKey.DependencyProperty;

    private bool _isRendering;
    private bool _stateTransitionActive;
    private long _stateTransitionStartedAt;
    private long _lightAnimationStartedAt;
    private LightAnimationMode _lightMode;

    private double _angleFrom;
    private double _angleTo;
    private double _shineFrom;
    private double _shineTo;
    private double _shadowFrom;
    private double _shadowTo;
    private double _glowFrom;
    private double _glowTo;

    private AxisAngleRotation3D? _rockerRotation;
    private FrameworkElement? _shineVisual;
    private FrameworkElement? _shadowVisual;
    private FrameworkElement? _glowVisual;
    private FrameworkElement? _lightVisual;

    static CheemsRockerSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsRockerSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsRockerSwitch)));
    }

    public CheemsRockerSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public double RockerAngle => (double)GetValue(RockerAngleProperty);
    public double ShineOpacity => (double)GetValue(ShineOpacityProperty);
    public double ShadowOpacity => (double)GetValue(ShadowOpacityProperty);
    public double GlowOpacity => (double)GetValue(GlowOpacityProperty);
    public double LightOpacity => (double)GetValue(LightOpacityProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rockerRotation = Template.FindName("PART_RockerRotation", this) as AxisAngleRotation3D;
        _shineVisual = Template.FindName("PART_Shine", this) as FrameworkElement;
        _shadowVisual = Template.FindName("PART_Shadow", this) as FrameworkElement;
        _glowVisual = Template.FindName("PART_Glow", this) as FrameworkElement;
        _lightVisual = Template.FindName("PART_Light", this) as FrameworkElement;
        UpdateTemplateVisuals();
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        BeginStateTransition();
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        BeginStateTransition();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
        ApplyFinalState();

        if (IsChecked == true && SystemParameters.ClientAreaAnimation)
        {
            SetValue(LightOpacityPropertyKey, 0.0);
            _lightMode = LightAnimationMode.Checked;
            _lightAnimationStartedAt = Stopwatch.GetTimestamp();
            EnsureRendering();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
    }

    private void BeginStateTransition()
    {
        var isChecked = IsChecked == true;
        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            StopRendering();
            ApplyFinalState();
            return;
        }

        _angleFrom = RockerAngle;
        _angleTo = isChecked ? 25.0 : -25.0;
        _shineFrom = ShineOpacity;
        _shineTo = isChecked ? 1.0 : 0.3;
        _shadowFrom = ShadowOpacity;
        _shadowTo = isChecked ? 0.0 : 1.0;
        _glowFrom = GlowOpacity;
        _glowTo = isChecked ? 1.0 : 0.0;
        _stateTransitionStartedAt = Stopwatch.GetTimestamp();
        _stateTransitionActive = true;

        _lightAnimationStartedAt = _stateTransitionStartedAt;
        _lightMode = isChecked ? LightAnimationMode.Checked : LightAnimationMode.Unchecked;
        SetValue(LightOpacityPropertyKey, isChecked ? 0.0 : 1.0);
        EnsureRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        UpdateStateTransition(now);
        UpdateLightAnimation(now);

        if (!_stateTransitionActive && _lightMode == LightAnimationMode.None)
        {
            StopRendering();
        }
    }

    private void UpdateStateTransition(long now)
    {
        if (!_stateTransitionActive)
        {
            return;
        }

        var elapsed = (now - _stateTransitionStartedAt) / (double)Stopwatch.Frequency;
        var progress = Math.Clamp(elapsed / StateTransitionSeconds, 0.0, 1.0);
        var eased = CubicBezier(progress, 1.0, 0.0, 1.0, 1.0);
        SetValue(RockerAnglePropertyKey, Lerp(_angleFrom, _angleTo, eased));
        SetValue(ShineOpacityPropertyKey, Lerp(_shineFrom, _shineTo, eased));
        SetValue(ShadowOpacityPropertyKey, Lerp(_shadowFrom, _shadowTo, eased));
        SetValue(GlowOpacityPropertyKey, Lerp(_glowFrom, _glowTo, eased));
        UpdateTemplateVisuals();

        if (progress < 1.0)
        {
            return;
        }

        SetValue(RockerAnglePropertyKey, _angleTo);
        SetValue(ShineOpacityPropertyKey, _shineTo);
        SetValue(ShadowOpacityPropertyKey, _shadowTo);
        SetValue(GlowOpacityPropertyKey, _glowTo);
        UpdateTemplateVisuals();
        _stateTransitionActive = false;
    }

    private void UpdateLightAnimation(long now)
    {
        var elapsed = (now - _lightAnimationStartedAt) / (double)Stopwatch.Frequency;
        switch (_lightMode)
        {
            case LightAnimationMode.Checked:
                if (elapsed < FlickerDelaySeconds)
                {
                    SetValue(LightOpacityPropertyKey, 0.0);
                    UpdateLightVisual();
                    return;
                }

                var phase = ((elapsed - FlickerDelaySeconds) % FlickerDurationSeconds) / FlickerDurationSeconds;
                if (phase <= 0.8)
                {
                    var progress = CubicBezier(phase / 0.8, 0.25, 0.1, 0.25, 1.0);
                    SetValue(LightOpacityPropertyKey, Lerp(1.0, 0.8, progress));
                }
                else
                {
                    var progress = CubicBezier((phase - 0.8) / 0.2, 0.25, 0.1, 0.25, 1.0);
                    SetValue(LightOpacityPropertyKey, Lerp(0.8, 1.0, progress));
                }

                UpdateLightVisual();

                break;

            case LightAnimationMode.Unchecked:
                if (elapsed < LightOffDurationSeconds * LightOffFadeEnd)
                {
                    var progress = elapsed / (LightOffDurationSeconds * LightOffFadeEnd);
                    var eased = CubicBezier(progress, 0.25, 0.1, 0.25, 1.0);
                    SetValue(LightOpacityPropertyKey, Lerp(1.0, 0.0, eased));
                    UpdateLightVisual();
                    return;
                }

                SetValue(LightOpacityPropertyKey, 0.0);
                UpdateLightVisual();
                if (elapsed >= LightOffDurationSeconds)
                {
                    _lightMode = LightAnimationMode.None;
                }

                break;
        }
    }

    private void ApplyFinalState()
    {
        var isChecked = IsChecked == true;
        SetValue(RockerAnglePropertyKey, isChecked ? 25.0 : -25.0);
        SetValue(ShineOpacityPropertyKey, isChecked ? 1.0 : 0.3);
        SetValue(ShadowOpacityPropertyKey, isChecked ? 0.0 : 1.0);
        SetValue(GlowOpacityPropertyKey, isChecked ? 1.0 : 0.0);
        SetValue(LightOpacityPropertyKey, isChecked ? 1.0 : 0.0);
        UpdateTemplateVisuals();
    }

    private void UpdateTemplateVisuals()
    {
        if (_rockerRotation is not null)
        {
            _rockerRotation.Angle = RockerAngle;
        }

        if (_shineVisual is not null)
        {
            _shineVisual.Opacity = ShineOpacity;
        }

        if (_shadowVisual is not null)
        {
            _shadowVisual.Opacity = ShadowOpacity;
        }

        if (_glowVisual is not null)
        {
            _glowVisual.Opacity = GlowOpacity;
        }

        UpdateLightVisual();
    }

    private void UpdateLightVisual()
    {
        if (_lightVisual is not null)
        {
            _lightVisual.Opacity = LightOpacity;
        }
    }

    private void EnsureRendering()
    {
        if (_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
    }

    private void StopRendering()
    {
        if (_isRendering)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRendering = false;
        }

        _stateTransitionActive = false;
        _lightMode = LightAnimationMode.None;
    }

    private static DependencyPropertyKey RegisterReadOnlyDouble(string name, double defaultValue) =>
        DependencyProperty.RegisterReadOnly(
            name,
            typeof(double),
            typeof(CheemsRockerSwitch),
            new FrameworkPropertyMetadata(defaultValue));

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        var parameter = x;

        for (var index = 0; index < 8; index++)
        {
            var error = SampleCurve(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001)
            {
                break;
            }

            parameter = Math.Clamp(parameter - error / derivative, 0.0, 1.0);
        }

        var low = 0.0;
        var high = 1.0;
        for (var index = 0; index < 14; index++)
        {
            var sampled = SampleCurve(parameter, x1, x2);
            if (Math.Abs(sampled - x) < 0.000001)
            {
                break;
            }

            if (sampled < x)
            {
                low = parameter;
            }
            else
            {
                high = parameter;
            }

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

    private enum LightAnimationMode
    {
        None,
        Checked,
        Unchecked
    }
}
