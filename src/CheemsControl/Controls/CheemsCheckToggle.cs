using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse mobinkakei 勾选/关闭切换器的 WPF 等价实现。
/// </summary>
/// <remarks>
/// 原版转换记录：72×36 轨道；<c>::after</c> 为同一个 26×26 滑块，中心从 (18,18) 到 (54,18)；
/// 两个 SVG 共享滑块坐标，并依原版通过尺寸/透明度切换。位移与图标为 0.15s ease-in-out，
/// 滑块颜色为 0.2s ease-out，滑块尺寸脉冲为 0.15s ease-out（26→20→26）。
/// </remarks>
[TemplatePart(Name = PartIndicatorName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartThumbName, Type = typeof(Border))]
[TemplatePart(Name = PartOnIconName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartOffIconName, Type = typeof(FrameworkElement))]
public sealed class CheemsCheckToggle : ToggleButton
{
    private const string PartIndicatorName = "PartIndicator";
    private const string PartThumbName = "PartThumb";
    private const string PartOnIconName = "PartOnIcon";
    private const string PartOffIconName = "PartOffIcon";
    private const double PositionDurationSeconds = 0.15;
    private const double ColorDurationSeconds = 0.2;
    private const double PulseDurationSeconds = 0.15;
    private const double ThumbMinimumScale = 20d / 26d;
    private const double IndicatorTravel = 36;

    private readonly TransitionChannel _position = new();
    private readonly TransitionChannel _color = new();
    private readonly TransitionChannel _icon = new();

    private FrameworkElement? _indicator;
    private FrameworkElement? _onIcon;
    private FrameworkElement? _offIcon;
    private SolidColorBrush? _thumbBrush;
    private TranslateTransform? _indicatorTranslation;
    private ScaleTransform? _thumbScale;
    private ScaleTransform? _onIconScale;
    private ScaleTransform? _offIconScale;
    private Color _offColor;
    private Color _onColor;
    private long _pulseStartedAt;
    private bool _templateApplied;
    private bool _renderingSubscribed;

    static CheemsCheckToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCheckToggle),
            new FrameworkPropertyMetadata(typeof(CheemsCheckToggle)));
    }

    public CheemsCheckToggle()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _indicator = GetTemplateChild(PartIndicatorName) as FrameworkElement;
        var thumb = GetTemplateChild(PartThumbName) as Border;
        _onIcon = GetTemplateChild(PartOnIconName) as FrameworkElement;
        _offIcon = GetTemplateChild(PartOffIconName) as FrameworkElement;

        _indicatorTranslation = InstallTranslation(_indicator);
        _thumbScale = InstallScale(thumb);
        _onIconScale = InstallScale(_onIcon);
        _offIconScale = InstallScale(_offIcon);

        _thumbBrush = (thumb?.Background as SolidColorBrush)?.Clone();
        if (thumb is not null && _thumbBrush is not null)
        {
            thumb.Background = _thumbBrush;
        }

        _offColor = GetBrushColor(CheemsKeys.CheckToggleThumbOffBrush, _thumbBrush?.Color ?? Colors.Transparent);
        _onColor = GetBrushColor(CheemsKeys.CheckToggleThumbOnBrush, _offColor);
        _templateApplied = true;
        SetImmediate(IsChecked == true ? 1 : 0);

        if (IsLoaded && SystemParameters.ClientAreaAnimation)
        {
            BeginPulse();
        }
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        StartTransition(1);
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        StartTransition(0);
    }

    protected override void OnIndeterminate(RoutedEventArgs e)
    {
        base.OnIndeterminate(e);
        StartTransition(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_templateApplied && SystemParameters.ClientAreaAnimation)
        {
            BeginPulse();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void StartTransition(double target)
    {
        if (!_templateApplied)
        {
            SetChannelsWithoutVisual(target);
            return;
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            SetImmediate(target);
            return;
        }

        var now = Stopwatch.GetTimestamp();
        _position.Update(now);
        _color.Update(now);
        _icon.Update(now);
        _position.Start(target, now, PositionDurationSeconds, 0.42, 0, 0.58, 1);
        _color.Start(target, now, ColorDurationSeconds, 0, 0, 0.58, 1);
        _icon.Start(target, now, PositionDurationSeconds, 0.42, 0, 0.58, 1);
        _pulseStartedAt = now;
        ApplyFrame(now);
        SubscribeRendering();
    }

    private void BeginPulse()
    {
        _pulseStartedAt = Stopwatch.GetTimestamp();
        ApplyFrame(_pulseStartedAt);
        SubscribeRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        _position.Update(now);
        _color.Update(now);
        _icon.Update(now);
        ApplyFrame(now);
        if (!HasActiveAnimation(now))
        {
            UnsubscribeRendering();
        }
    }

    private void ApplyFrame(long now)
    {
        if (_indicatorTranslation is not null)
        {
            _indicatorTranslation.X = IndicatorTravel * _position.Value;
        }

        if (_thumbBrush is not null)
        {
            _thumbBrush.Color = Lerp(_offColor, _onColor, _color.Value);
        }

        var thumbScale = GetPulseScale(now);
        if (_thumbScale is not null)
        {
            _thumbScale.ScaleX = thumbScale;
            _thumbScale.ScaleY = thumbScale;
        }

        var onOpacity = _icon.Value;
        var offOpacity = 1 - onOpacity;
        ApplyIcon(_onIcon, _onIconScale, onOpacity);
        ApplyIcon(_offIcon, _offIconScale, offOpacity);
    }

    private void SetImmediate(double value)
    {
        _position.Set(value);
        _color.Set(value);
        _icon.Set(value);
        _pulseStartedAt = 0;
        ApplyFrame(0);
        UnsubscribeRendering();
    }

    private void SetChannelsWithoutVisual(double value)
    {
        _position.Set(value);
        _color.Set(value);
        _icon.Set(value);
    }

    private bool HasActiveAnimation(long now)
    {
        return _position.IsActive || _color.IsActive || _icon.IsActive || IsPulsing(now);
    }

    private bool IsPulsing(long now)
    {
        return _pulseStartedAt != 0 &&
               (now - _pulseStartedAt) / (double)Stopwatch.Frequency < PulseDurationSeconds;
    }

    private double GetPulseScale(long now)
    {
        if (!IsPulsing(now))
        {
            return 1;
        }

        var elapsed = (now - _pulseStartedAt) / (double)Stopwatch.Frequency;
        if (elapsed <= PulseDurationSeconds / 2)
        {
            return Lerp(1, ThumbMinimumScale, EaseOut(elapsed / (PulseDurationSeconds / 2)));
        }

        return Lerp(ThumbMinimumScale, 1, EaseOut((elapsed - (PulseDurationSeconds / 2)) / (PulseDurationSeconds / 2)));
    }

    private Color GetBrushColor(string key, Color fallback)
    {
        return TryFindResource(key) is SolidColorBrush brush ? brush.Color : fallback;
    }

    private static TranslateTransform? InstallTranslation(FrameworkElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private static ScaleTransform? InstallScale(FrameworkElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var transform = new ScaleTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private static void ApplyIcon(FrameworkElement? icon, ScaleTransform? scale, double opacity)
    {
        if (icon is not null)
        {
            icon.Opacity = opacity;
        }

        if (scale is not null)
        {
            scale.ScaleX = opacity;
            scale.ScaleY = opacity;
        }
    }

    private static double EaseOut(double value) => CubicBezier(value, 0, 0, 0.58, 1);

    private static double CubicBezier(double value, double x1, double y1, double x2, double y2)
    {
        var lower = 0.0;
        var upper = 1.0;
        var parameter = value;
        for (var index = 0; index < 12; index++)
        {
            parameter = (lower + upper) / 2;
            if (Cubic(parameter, x1, x2) < value) lower = parameter;
            else upper = parameter;
        }

        return Cubic(parameter, y1, y2);
    }

    private static double Cubic(double value, double first, double second)
    {
        var inverse = 1 - value;
        return 3 * inverse * inverse * value * first +
               3 * inverse * value * value * second +
               value * value * value;
    }

    private static Color Lerp(Color from, Color to, double progress)
    {
        return Color.FromArgb(
            (byte)Math.Round(from.A + (to.A - from.A) * progress),
            (byte)Math.Round(from.R + (to.R - from.R) * progress),
            (byte)Math.Round(from.G + (to.G - from.G) * progress),
            (byte)Math.Round(from.B + (to.B - from.B) * progress));
    }

    private static double Lerp(double from, double to, double progress) => from + (to - from) * progress;

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

    private sealed class TransitionChannel
    {
        private double _from;
        private double _target;
        private long _startedAt;
        private double _duration;
        private double _x1;
        private double _y1;
        private double _x2;
        private double _y2;

        public double Value { get; private set; }
        public bool IsActive { get; private set; }

        public void Set(double value)
        {
            Value = value;
            _from = value;
            _target = value;
            IsActive = false;
        }

        public void Start(double target, long startedAt, double duration, double x1, double y1, double x2, double y2)
        {
            _from = Value;
            _target = target;
            _startedAt = startedAt;
            _duration = duration;
            _x1 = x1;
            _y1 = y1;
            _x2 = x2;
            _y2 = y2;
            IsActive = Math.Abs(_from - target) > 0.000001;
        }

        public void Update(long now)
        {
            if (!IsActive)
            {
                return;
            }

            var time = Math.Clamp((now - _startedAt) / (double)Stopwatch.Frequency / _duration, 0, 1);
            Value = Lerp(_from, _target, CubicBezier(time, _x1, _y1, _x2, _y2));
            if (time >= 1)
            {
                Value = _target;
                IsActive = false;
            }
        }
    }
}
