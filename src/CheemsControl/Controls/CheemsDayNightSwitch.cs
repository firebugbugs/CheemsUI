using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CheemsControl;

/// <summary>
/// Uiverse Mohammad-Rahme-576 昼夜开关的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartDayTrackName, Type = typeof(Border))]
[TemplatePart(Name = PartNightTrackName, Type = typeof(Border))]
[TemplatePart(Name = PartStarsName, Type = typeof(Grid))]
[TemplatePart(Name = PartThumbName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartThumbFaceName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartDiscName, Type = typeof(Ellipse))]
[TemplatePart(Name = PartMoonEffectName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartMoonLightName, Type = typeof(Ellipse))]
[TemplatePart(Name = PartPseudoBeforeName, Type = typeof(Ellipse))]
[TemplatePart(Name = PartPseudoAfterName, Type = typeof(Ellipse))]
public sealed class CheemsDayNightSwitch : ToggleButton
{
    private const string PartDayTrackName = "PartDayTrack";
    private const string PartNightTrackName = "PartNightTrack";
    private const string PartDayHoverName = "PartDayHover";
    private const string PartNightHoverName = "PartNightHover";
    private const string PartStarsName = "PartStars";
    private const string PartStarOneName = "PartStarOne";
    private const string PartStarTwoName = "PartStarTwo";
    private const string PartThumbName = "PartThumb";
    private const string PartThumbFaceName = "PartThumbFace";
    private const string PartSunPulseNearName = "PartSunPulseNear";
    private const string PartSunPulseWideName = "PartSunPulseWide";
    private const string PartDiscName = "PartDisc";
    private const string PartMoonEffectName = "PartMoonEffect";
    private const string PartMoonLightName = "PartMoonLight";
    private const string PartMoonGlowName = "PartMoonGlow";
    private const string PartPseudoBeforeName = "PartPseudoBefore";
    private const string PartPseudoAfterName = "PartPseudoAfter";
    private const string PartHighContrastBorderName = "PartHighContrastBorder";
    private const double SunPulseNearSpread = 22.0;
    private const double SunPulseWideSpread = 44.0;

    private const double TransitionSeconds = 0.6;
    private const double ThumbStart = 5.4;
    private const double ThumbTravel = 54.0;
    private const double PerspectiveScale = 500.0 / (500.0 - 5.0);

    private Border? _dayTrack;
    private Border? _nightTrack;
    private Border? _dayHover;
    private Border? _nightHover;
    private Grid? _stars;
    private Ellipse? _starOne;
    private Ellipse? _starTwo;
    private FrameworkElement? _thumb;
    private FrameworkElement? _thumbFace;
    private FrameworkElement? _sunPulseNear;
    private FrameworkElement? _sunPulseWide;
    private Ellipse? _disc;
    private FrameworkElement? _moonEffect;
    private Ellipse? _moonLight;
    private FrameworkElement? _moonGlow;
    private Ellipse? _pseudoBefore;
    private Ellipse? _pseudoAfter;
    private Border? _highContrastBorder;

    private ScaleTransform? _faceScale;
    private ScaleTransform? _nearScale;
    private ScaleTransform? _wideScale;
    private TranslateTransform? _moonTranslate;
    private SolidColorBrush? _discBrush;
    private SolidColorBrush? _beforeBrush;
    private SolidColorBrush? _afterBrush;

    private Color _sunColor;
    private Color _moonColor;
    private Color _cloudColor;
    private Color _craterStrongColor;
    private Color _craterFaintColor;

    private double _visualProgress;
    private double _morphProgress;
    private double _fromVisualProgress;
    private double _fromMorphProgress;
    private double _targetProgress;
    private long _transitionStart;
    private long _ambientStart;
    private bool _transitionActive;
    private bool _renderingSubscribed;

    static CheemsDayNightSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsDayNightSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsDayNightSwitch)));
    }

    public CheemsDayNightSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _dayTrack = GetTemplateChild(PartDayTrackName) as Border;
        _nightTrack = GetTemplateChild(PartNightTrackName) as Border;
        _dayHover = GetTemplateChild(PartDayHoverName) as Border;
        _nightHover = GetTemplateChild(PartNightHoverName) as Border;
        _stars = GetTemplateChild(PartStarsName) as Grid;
        _starOne = GetTemplateChild(PartStarOneName) as Ellipse;
        _starTwo = GetTemplateChild(PartStarTwoName) as Ellipse;
        _thumb = GetTemplateChild(PartThumbName) as FrameworkElement;
        _thumbFace = GetTemplateChild(PartThumbFaceName) as FrameworkElement;
        _sunPulseNear = GetTemplateChild(PartSunPulseNearName) as FrameworkElement;
        _sunPulseWide = GetTemplateChild(PartSunPulseWideName) as FrameworkElement;
        _disc = GetTemplateChild(PartDiscName) as Ellipse;
        _moonEffect = GetTemplateChild(PartMoonEffectName) as FrameworkElement;
        _moonLight = GetTemplateChild(PartMoonLightName) as Ellipse;
        _moonGlow = GetTemplateChild(PartMoonGlowName) as FrameworkElement;
        _pseudoBefore = GetTemplateChild(PartPseudoBeforeName) as Ellipse;
        _pseudoAfter = GetTemplateChild(PartPseudoAfterName) as Ellipse;
        _highContrastBorder = GetTemplateChild(PartHighContrastBorderName) as Border;

        // 模板中的 Freezable 会被 WPF 自动冻结；逐帧动画必须使用控件实例自己的可写副本。
        _faceScale = CloneScaleTransform(_thumbFace);
        _nearScale = CloneScaleTransform(_sunPulseNear);
        _wideScale = CloneScaleTransform(_sunPulseWide);
        _moonTranslate = CloneTranslateTransform(_moonLight);

        _sunColor = GetColor(CheemsKeys.DayNightSunColor, Color.FromRgb(0xFF, 0xD7, 0x00));
        _moonColor = GetColor(CheemsKeys.DayNightMoonColor, Colors.White);
        _cloudColor = GetColor(CheemsKeys.DayNightCloudColor, Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
        _craterStrongColor = GetColor(CheemsKeys.DayNightCraterStrongColor, Color.FromArgb(0x33, 0, 0, 0));
        _craterFaintColor = GetColor(CheemsKeys.DayNightCraterFaintColor, Color.FromArgb(0x26, 0, 0, 0));

        _discBrush = ReplaceFill(_disc, _sunColor);
        _beforeBrush = ReplaceFill(_pseudoBefore, _cloudColor);
        _afterBrush = ReplaceFill(_pseudoAfter, _cloudColor);

        ApplyHighContrastMode();

        _targetProgress = IsChecked == true ? 1.0 : 0.0;
        _visualProgress = _targetProgress;
        _morphProgress = _targetProgress;
        _transitionActive = false;
        _ambientStart = Stopwatch.GetTimestamp();
        ApplyFrame();
        ApplyAmbientFrame(0.0);
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        StartTransition(1.0);
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        StartTransition(0.0);
    }

    protected override void OnIndeterminate(RoutedEventArgs e)
    {
        base.OnIndeterminate(e);
        StartTransition(0.0);
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        ApplyFrame();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ApplyFrame();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ambientStart = Stopwatch.GetTimestamp();
        SubscribeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeRendering();
    }

    private void StartTransition(double target)
    {
        _targetProgress = target;
        _ambientStart = Stopwatch.GetTimestamp();

        if (_thumb is null)
        {
            _visualProgress = target;
            _morphProgress = target;
            return;
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            _visualProgress = target;
            _morphProgress = target;
            _transitionActive = false;
            ApplyFrame();
            ApplyAmbientFrame(0.0);
            return;
        }

        _fromVisualProgress = _visualProgress;
        _fromMorphProgress = _morphProgress;
        _transitionStart = Stopwatch.GetTimestamp();
        _transitionActive = true;
        SubscribeRendering();
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed || !IsLoaded)
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
        long now = Stopwatch.GetTimestamp();

        if (_transitionActive)
        {
            double elapsed = ElapsedSeconds(_transitionStart, now);
            double time = Clamp01(elapsed / TransitionSeconds);

            // .slider/.slider-inner: cubic-bezier(.68,-.55,.265,1.55)
            double spring = CubicBezier(time, 0.68, -0.55, 0.265, 1.55);
            // ::before/::after: CSS ease = cubic-bezier(.25,.1,.25,1)
            double ease = CubicBezier(time, 0.25, 0.1, 0.25, 1.0);
            _visualProgress = Lerp(_fromVisualProgress, _targetProgress, spring);
            _morphProgress = Lerp(_fromMorphProgress, _targetProgress, ease);

            if (time >= 1.0)
            {
                _visualProgress = _targetProgress;
                _morphProgress = _targetProgress;
                _transitionActive = false;
            }

            ApplyFrame();
        }

        double ambientSeconds = ElapsedSeconds(_ambientStart, now);
        ApplyAmbientFrame(ambientSeconds);
    }

    private void ApplyFrame()
    {
        double state = Clamp01(_visualProgress);
        double morph = Clamp01(_morphProgress);

        if (_dayTrack is not null) _dayTrack.Opacity = 1.0 - state;
        if (_nightTrack is not null) _nightTrack.Opacity = state;

        // hover 只换成源码定义的 hover 渐变，不引入额外颜色。
        bool hovered = IsMouseOver;
        if (_dayHover is not null) _dayHover.Opacity = hovered ? 1.0 - state : 0.0;
        if (_nightHover is not null) _nightHover.Opacity = hovered ? state : 0.0;

        if (_stars is not null) _stars.Opacity = morph;
        if (_thumb is not null) Canvas.SetLeft(_thumb, ThumbStart + ThumbTravel * _visualProgress);

        // perspective:500px + translateZ(5px) + rotateY(180deg)。
        // WPF 2D 模板用 cos 投影保持原版“中途压扁，结束水平翻面”的视觉轨迹。
        if (_faceScale is not null)
        {
            _faceScale.ScaleX = PerspectiveScale * Math.Cos(Math.PI * _visualProgress);
            _faceScale.ScaleY = PerspectiveScale;
        }

        if (!SystemParameters.HighContrast)
        {
            if (_discBrush is not null) _discBrush.Color = Interpolate(_sunColor, _moonColor, state);
            if (_beforeBrush is not null) _beforeBrush.Color = Interpolate(_cloudColor, _craterStrongColor, morph);
            if (_afterBrush is not null) _afterBrush.Color = Interpolate(_cloudColor, _craterFaintColor, morph);
        }

        if (_moonEffect is not null) _moonEffect.Opacity = state;

        // ::before: 1em/-0.5em/-0.2em -> .6em/.3em/.3em
        SetEllipseGeometry(
            _pseudoBefore,
            Lerp(18.0, 10.8, morph),
            Lerp(18.0, 10.8, morph),
            Lerp(-3.6, 5.4, morph),
            Lerp(-9.0, 5.4, morph));

        // ::after: 1.2em/bottom-.6em/right-.3em -> .4em/bottom.5em/right.5em
        SetEllipseGeometry(
            _pseudoAfter,
            Lerp(21.6, 7.2, morph),
            Lerp(21.6, 7.2, morph),
            Lerp(27.0, 27.0, morph),
            Lerp(32.4, 27.0, morph));
    }

    private void ApplyAmbientFrame(double seconds)
    {
        double state = Clamp01(_visualProgress);

        if (!SystemParameters.ClientAreaAnimation)
        {
            if (_sunPulseNear is not null) _sunPulseNear.Opacity = 0;
            if (_sunPulseWide is not null) _sunPulseWide.Opacity = 0;
            if (_moonTranslate is not null)
            {
                _moonTranslate.X = 10;
                _moonTranslate.Y = -5;
            }
            if (_moonGlow is not null) _moonGlow.Opacity = state;
            if (_starOne is not null) _starOne.Opacity = 1;
            if (_starTwo is not null) _starTwo.Opacity = 1;
            return;
        }

        // sunPulse 3s infinite：保留双层脉冲，将最大扩散范围收窄，避免覆盖大半条轨道。
        double sunPulse = PulseEase(seconds / 3.0);
        if (_nearScale is not null)
        {
            _nearScale.ScaleX = 1.0 + (SunPulseNearSpread / 43.2) * 2.0 * sunPulse;
            _nearScale.ScaleY = _nearScale.ScaleX;
        }
        if (_wideScale is not null)
        {
            _wideScale.ScaleX = 1.0 + (SunPulseWideSpread / 43.2) * 2.0 * sunPulse;
            _wideScale.ScaleY = _wideScale.ScaleX;
        }
        if (_sunPulseNear is not null) _sunPulseNear.Opacity = sunPulse * (1.0 - state);
        if (_sunPulseWide is not null) _sunPulseWide.Opacity = sunPulse * (1.0 - state);

        // moonPhase 5s infinite: inset -10px -5px 在 50% 移动到 0,0；白色外发光恒定。
        double moonPhase = PulseEase(seconds / 5.0);
        if (_moonTranslate is not null)
        {
            _moonTranslate.X = 10.0 * (1.0 - moonPhase);
            _moonTranslate.Y = -5.0 * (1.0 - moonPhase);
        }
        if (_moonGlow is not null) _moonGlow.Opacity = state;

        // twinkle 2s infinite；::before 延迟 .5s，::after 无延迟。
        if (_starOne is not null) _starOne.Opacity = 0.2 + 0.8 * PulseEase((seconds - 0.5) / 2.0);
        if (_starTwo is not null) _starTwo.Opacity = 0.2 + 0.8 * PulseEase(seconds / 2.0);
    }

    private void ApplyHighContrastMode()
    {
        if (!SystemParameters.HighContrast)
        {
            if (_highContrastBorder is not null) _highContrastBorder.Opacity = 0;
            return;
        }

        if (_dayTrack is not null) _dayTrack.Background = SystemColors.WindowBrush;
        if (_nightTrack is not null) _nightTrack.Background = SystemColors.HighlightBrush;
        if (_dayHover is not null) _dayHover.Background = SystemColors.WindowBrush;
        if (_nightHover is not null) _nightHover.Background = SystemColors.HighlightBrush;
        if (_disc is not null) _disc.Fill = SystemColors.ControlBrush;
        if (_pseudoBefore is not null) _pseudoBefore.Fill = SystemColors.ControlTextBrush;
        if (_pseudoAfter is not null) _pseudoAfter.Fill = SystemColors.ControlTextBrush;
        if (_starOne is not null) _starOne.Fill = SystemColors.ControlTextBrush;
        if (_starTwo is not null) _starTwo.Fill = SystemColors.ControlTextBrush;
        if (_highContrastBorder is not null)
        {
            _highContrastBorder.BorderBrush = SystemColors.ControlTextBrush;
            _highContrastBorder.Opacity = 1;
        }
    }

    private Color GetColor(string key, Color fallback)
    {
        object resource = TryFindResource(key);
        return resource is Color color ? color : fallback;
    }

    private static SolidColorBrush? ReplaceFill(Shape? shape, Color color)
    {
        if (shape is null)
        {
            return null;
        }

        var brush = new SolidColorBrush(color);
        shape.Fill = brush;
        return brush;
    }

    private static ScaleTransform? CloneScaleTransform(FrameworkElement? element)
    {
        if (element?.RenderTransform is not ScaleTransform source)
        {
            return null;
        }

        ScaleTransform clone = source.CloneCurrentValue();
        element.RenderTransform = clone;
        return clone;
    }

    private static TranslateTransform? CloneTranslateTransform(FrameworkElement? element)
    {
        if (element?.RenderTransform is not TranslateTransform source)
        {
            return null;
        }

        TranslateTransform clone = source.CloneCurrentValue();
        element.RenderTransform = clone;
        return clone;
    }

    private static void SetEllipseGeometry(Ellipse? ellipse, double width, double height, double left, double top)
    {
        if (ellipse is null)
        {
            return;
        }

        ellipse.Width = width;
        ellipse.Height = height;
        Canvas.SetLeft(ellipse, left);
        Canvas.SetTop(ellipse, top);
    }

    private static double ElapsedSeconds(long start, long now) =>
        (now - start) / (double)Stopwatch.Frequency;

    private static double Clamp01(double value) => Math.Max(0.0, Math.Min(1.0, value));

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;

    private static Color Interpolate(Color from, Color to, double progress)
    {
        progress = Clamp01(progress);
        return Color.FromArgb(
            (byte)Math.Round(Lerp(from.A, to.A, progress)),
            (byte)Math.Round(Lerp(from.R, to.R, progress)),
            (byte)Math.Round(Lerp(from.G, to.G, progress)),
            (byte)Math.Round(Lerp(from.B, to.B, progress)));
    }

    private static double PulseEase(double cycles)
    {
        double phase = cycles - Math.Floor(cycles);
        return phase <= 0.5
            ? CubicBezier(phase * 2.0, 0.25, 0.1, 0.25, 1.0)
            : 1.0 - CubicBezier((phase - 0.5) * 2.0, 0.25, 0.1, 0.25, 1.0);
    }

    /// <summary>按 CSS 定义求 cubic-bezier(x1,y1,x2,y2) 在给定时间 x 的 y。</summary>
    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        x = Clamp01(x);
        double parameter = x;

        for (int i = 0; i < 8; i++)
        {
            double error = SampleCurve(parameter, x1, x2) - x;
            double derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001)
            {
                break;
            }

            parameter -= error / derivative;
            if (parameter < 0.0 || parameter > 1.0)
            {
                parameter = Clamp01(parameter);
                break;
            }
        }

        double low = 0.0;
        double high = 1.0;
        for (int i = 0; i < 12; i++)
        {
            double sampledX = SampleCurve(parameter, x1, x2);
            if (Math.Abs(sampledX - x) < 0.000001)
            {
                break;
            }

            if (sampledX < x) low = parameter;
            else high = parameter;
            parameter = (low + high) * 0.5;
        }

        return SampleCurve(parameter, y1, y2);
    }

    private static double SampleCurve(double t, double p1, double p2)
    {
        double inverse = 1.0 - t;
        return 3.0 * inverse * inverse * t * p1
             + 3.0 * inverse * t * t * p2
             + t * t * t;
    }

    private static double SampleDerivative(double t, double p1, double p2)
    {
        double inverse = 1.0 - t;
        return 3.0 * inverse * inverse * p1
             + 6.0 * inverse * t * (p2 - p1)
             + 3.0 * t * t * (1.0 - p2);
    }
}
