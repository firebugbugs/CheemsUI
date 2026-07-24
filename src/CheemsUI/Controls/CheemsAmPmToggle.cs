using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse mobinkakei AM/PM 切换器的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartNightTrackName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHandlerName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartNightHandlerName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCratersName, Type = typeof(FrameworkElement))]
public sealed class CheemsAmPmToggle : ToggleButton
{
    private const string PartNightTrackName = "PartNightTrack";
    private const string PartHandlerName = "PartHandler";
    private const string PartNightHandlerName = "PartNightHandler";
    private const string PartCratersName = "PartCraters";

    private readonly TransitionChannel _track = new();
    private readonly TransitionChannel _handler = new();
    private readonly TransitionChannel _craters = new();
    private readonly TransitionChannel _mainStars = new();
    private readonly TransitionChannel[] _delayedStars =
    {
        new(), new(), new(),
    };

    private FrameworkElement? _nightTrack;
    private FrameworkElement? _nightHandler;
    private FrameworkElement? _craterGroup;
    private FrameworkElement? _star1;
    private FrameworkElement? _star2;
    private FrameworkElement? _star3;
    private FrameworkElement? _star4;
    private FrameworkElement? _star5;
    private FrameworkElement? _star6;

    private RotateTransform? _handlerRotation;
    private TranslateTransform? _handlerTranslation;
    private TranslateTransform? _star2Translation;
    private TranslateTransform? _star3Translation;
    private TranslateTransform? _star4Translation;
    private TranslateTransform? _star5Translation;
    private TranslateTransform? _star6Translation;

    private bool _templateApplied;
    private bool _renderingSubscribed;

    static CheemsAmPmToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsAmPmToggle),
            new FrameworkPropertyMetadata(typeof(CheemsAmPmToggle)));
    }

    public CheemsAmPmToggle()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _nightTrack = GetTemplateChild(PartNightTrackName) as FrameworkElement;
        var handler = GetTemplateChild(PartHandlerName) as FrameworkElement;
        _nightHandler = GetTemplateChild(PartNightHandlerName) as FrameworkElement;
        _craterGroup = GetTemplateChild(PartCratersName) as FrameworkElement;
        _star1 = GetTemplateChild("PartStar1") as FrameworkElement;
        _star2 = GetTemplateChild("PartStar2") as FrameworkElement;
        _star3 = GetTemplateChild("PartStar3") as FrameworkElement;
        _star4 = GetTemplateChild("PartStar4") as FrameworkElement;
        _star5 = GetTemplateChild("PartStar5") as FrameworkElement;
        _star6 = GetTemplateChild("PartStar6") as FrameworkElement;

        if (handler is not null)
        {
            _handlerRotation = new RotateTransform(-45, 22, 22);
            _handlerTranslation = new TranslateTransform();
            var group = new TransformGroup();
            group.Children.Add(_handlerRotation);
            group.Children.Add(_handlerTranslation);
            handler.RenderTransform = group;
        }

        _star2Translation = InstallTranslation(_star2);
        _star3Translation = InstallTranslation(_star3);
        _star4Translation = InstallTranslation(_star4);
        _star5Translation = InstallTranslation(_star5);
        _star6Translation = InstallTranslation(_star6);

        _templateApplied = true;
        SetImmediate(IsChecked == true ? 1 : 0);
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
        if (HasActiveTransitions())
        {
            SubscribeRendering();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeRendering();
    }

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
        _track.Start(target, now, 0.2, 0, EasingKind.Standard);
        _handler.Start(target, now, 0.4, 0, EasingKind.Bounce);
        _craters.Start(target, now, 0.2, 0, EasingKind.EaseInOut);
        _mainStars.Start(target, now, 0.3, 0, EasingKind.Standard);

        // checked 规则声明了递增延迟；unchecked 恢复基础 transition，延迟为 0。
        _delayedStars[0].Start(target, now, 0.3, target > 0 ? 0.2 : 0, EasingKind.Standard);
        _delayedStars[1].Start(target, now, 0.3, target > 0 ? 0.3 : 0, EasingKind.Standard);
        _delayedStars[2].Start(target, now, 0.3, target > 0 ? 0.4 : 0, EasingKind.Standard);

        ApplyFrame();
        SubscribeRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        _track.Update(now);
        _handler.Update(now);
        _craters.Update(now);
        _mainStars.Update(now);
        foreach (var star in _delayedStars)
        {
            star.Update(now);
        }

        ApplyFrame();
        if (!HasActiveTransitions())
        {
            UnsubscribeRendering();
        }
    }

    private void ApplyFrame()
    {
        var track = Clamp01(_track.Value);
        var handler = _handler.Value;
        var handlerColor = Clamp01(handler);
        var craters = Clamp01(_craters.Value);
        var stars = Clamp01(_mainStars.Value);

        if (_nightTrack is not null) _nightTrack.Opacity = track;
        if (_nightHandler is not null) _nightHandler.Opacity = handlerColor;
        if (_craterGroup is not null) _craterGroup.Opacity = craters;

        if (_handlerTranslation is not null) _handlerTranslation.X = 40 * handler;
        if (_handlerRotation is not null) _handlerRotation.Angle = -45 * (1 - handler);

        SetSize(_star1, Lerp(30, 2, stars), Lerp(3, 2, stars));
        SetSize(_star2, Lerp(30, 4, stars), Lerp(3, 4, stars));
        SetSize(_star3, Lerp(30, 2, stars), Lerp(3, 2, stars));
        if (_star2Translation is not null) _star2Translation.X = -5 * stars;
        if (_star3Translation is not null) _star3Translation.X = -7 * stars;

        ApplyDelayedStar(_star4, _star4Translation, _delayedStars[0].Value);
        ApplyDelayedStar(_star5, _star5Translation, _delayedStars[1].Value);
        ApplyDelayedStar(_star6, _star6Translation, _delayedStars[2].Value);
    }

    private void SetImmediate(double value)
    {
        _track.Set(value);
        _handler.Set(value);
        _craters.Set(value);
        _mainStars.Set(value);
        foreach (var star in _delayedStars)
        {
            star.Set(value);
        }

        ApplyFrame();
        UnsubscribeRendering();
    }

    private void SetChannelsWithoutVisual(double value)
    {
        _track.Set(value);
        _handler.Set(value);
        _craters.Set(value);
        _mainStars.Set(value);
        foreach (var star in _delayedStars)
        {
            star.Set(value);
        }
    }

    private bool HasActiveTransitions()
    {
        if (_track.IsActive || _handler.IsActive || _craters.IsActive || _mainStars.IsActive)
        {
            return true;
        }

        return _delayedStars.Any(star => star.IsActive);
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

    private static void ApplyDelayedStar(
        FrameworkElement? element,
        TranslateTransform? transform,
        double progress)
    {
        progress = Clamp01(progress);
        if (element is not null) element.Opacity = progress;
        if (transform is not null) transform.X = 3 * (1 - progress);
    }

    private static void SetSize(FrameworkElement? element, double width, double height)
    {
        if (element is null)
        {
            return;
        }

        element.Width = width;
        element.Height = height;
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
    private static double Lerp(double from, double to, double progress) => from + (to - from) * progress;

    private enum EasingKind
    {
        Standard,
        EaseInOut,
        Bounce,
    }

    private sealed class TransitionChannel
    {
        private double _from;
        private double _target;
        private long _start;
        private double _duration;
        private double _delay;
        private EasingKind _easing;

        public double Value { get; private set; }
        public bool IsActive { get; private set; }

        public void Set(double value)
        {
            Value = value;
            _from = value;
            _target = value;
            IsActive = false;
        }

        public void Start(
            double target,
            long start,
            double duration,
            double delay,
            EasingKind easing)
        {
            _from = Value;
            _target = target;
            _start = start;
            _duration = duration;
            _delay = delay;
            _easing = easing;
            IsActive = Math.Abs(_from - target) > 0.000001;
        }

        public void Update(long now)
        {
            if (!IsActive)
            {
                return;
            }

            var elapsed = (now - _start) / (double)Stopwatch.Frequency;
            if (elapsed < _delay)
            {
                return;
            }

            var time = Math.Max(0, Math.Min(1, (elapsed - _delay) / _duration));
            var progress = _easing switch
            {
                EasingKind.Bounce => CubicBezier(time, 0.68, -0.55, 0.265, 1.55),
                EasingKind.EaseInOut => CubicBezier(time, 0.42, 0, 0.58, 1),
                _ => CubicBezier(time, 0.445, 0.05, 0.55, 0.95),
            };
            Value = Lerp(_from, _target, progress);

            if (time >= 1)
            {
                Value = _target;
                IsActive = false;
            }
        }

        private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
        {
            var parameter = x;
            for (var index = 0; index < 8; index++)
            {
                var error = Sample(parameter, x1, x2) - x;
                var derivative = Derivative(parameter, x1, x2);
                if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
                parameter = Math.Max(0, Math.Min(1, parameter - error / derivative));
            }

            var low = 0.0;
            var high = 1.0;
            for (var index = 0; index < 12; index++)
            {
                var sampled = Sample(parameter, x1, x2);
                if (Math.Abs(sampled - x) < 0.000001) break;
                if (sampled < x) low = parameter;
                else high = parameter;
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

        private static double Derivative(double time, double first, double second)
        {
            var inverse = 1 - time;
            return 3 * inverse * inverse * first
                 + 6 * inverse * time * (second - first)
                 + 3 * time * time * (1 - second);
        }
    }
}
