using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

[TemplatePart(Name = PartCoverName, Type = typeof(Border))]
[TemplatePart(Name = PartEyesName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartEyeOneName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartEyeTwoName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPupilOneName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPupilTwoName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartFocusOutlineName, Type = typeof(FrameworkElement))]
public sealed class CheemsCreepyButton : Button
{
    private const string PartCoverName = "PartCover";
    private const string PartEyesName = "PartEyes";
    private const string PartEyeOneName = "PartEyeOne";
    private const string PartEyeTwoName = "PartEyeTwo";
    private const string PartPupilOneName = "PartPupilOne";
    private const string PartPupilTwoName = "PartPupilTwo";
    private const string PartFocusOutlineName = "PartFocusOutline";
    private const double TransitionDurationSeconds = 0.3;
    private const double BlinkDurationSeconds = 3.0;
    private const double ReferenceWidth = 288.0;

    private Border? _cover;
    private FrameworkElement? _eyes;
    private FrameworkElement? _focusOutline;
    private RotateTransform? _coverRotation;
    private ScaleTransform? _eyeOneScale;
    private ScaleTransform? _eyeTwoScale;
    private TranslateTransform? _pupilOneTranslation;
    private TranslateTransform? _pupilTwoTranslation;
    private SolidColorBrush? _coverBrush;
    private Color _normalCoverColor;
    private Color _hoverCoverColor;
    private Color _transitionFromColor;
    private Color _transitionToColor;
    private double _currentAngle;
    private double _transitionFromAngle;
    private double _transitionToAngle;
    private bool _useOvershootEase;
    private long _transitionStartedAt;
    private long _blinkStartedAt;
    private bool _transitionActive;
    private bool _renderingSubscribed;
    private bool _keyboardFocusVisible;
    private bool _pointerFocusInProgress;

    static CheemsCreepyButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCreepyButton),
            new FrameworkPropertyMetadata(typeof(CheemsCreepyButton)));
    }

    public CheemsCreepyButton()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsEnabledChanged += OnIsEnabledChanged;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _cover = GetTemplateChild(PartCoverName) as Border;
        _eyes = GetTemplateChild(PartEyesName) as FrameworkElement;
        _focusOutline = GetTemplateChild(PartFocusOutlineName) as FrameworkElement;
        _coverRotation = InstallRotation(_cover, new Point(40.0 / ReferenceWidth, 0.5));
        _eyeOneScale = InstallScale(GetTemplateChild(PartEyeOneName) as FrameworkElement, new Point(0.5, 1));
        _eyeTwoScale = InstallScale(GetTemplateChild(PartEyeTwoName) as FrameworkElement, new Point(0.5, 1));
        _pupilOneTranslation = InstallTranslation(GetTemplateChild(PartPupilOneName) as FrameworkElement);
        _pupilTwoTranslation = InstallTranslation(GetTemplateChild(PartPupilTwoName) as FrameworkElement);

        _normalCoverColor = GetBrushColor(_cover?.Background);
        _hoverCoverColor = TryFindResource(CheemsKeys.CreepyButtonCoverHoverColor) is Color hoverColor
            ? hoverColor
            : _normalCoverColor;
        _coverBrush = new SolidColorBrush(_normalCoverColor);
        if (_cover is not null) _cover.Background = _coverBrush;

        ApplyStateImmediately();
        ApplyBlink(1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        UpdatePupils(e.GetPosition(this));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // 视觉状态由属性本身驱动，而不是依赖某一次鼠标路由事件。
        // 这样既能正确处理中途失去捕获，也不会在点击后因焦点残留卡在展开状态。
        if (e.Property == IsMouseOverProperty || e.Property == IsPressedProperty)
        {
            BeginStateTransition();
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // ButtonBase 会在 base 调用中获取键盘焦点；先标记输入来源，避免把
        // 鼠标点击产生的焦点误判成 CSS 的 :focus-visible。
        _pointerFocusInProgress = true;
        try
        {
            base.OnMouseLeftButtonDown(e);
        }
        finally
        {
            _pointerFocusInProgress = false;
        }

        HideKeyboardFocusVisual();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        _keyboardFocusVisible = !_pointerFocusInProgress &&
                                InputManager.Current.MostRecentInputDevice is KeyboardDevice;
        if (_focusOutline is not null)
        {
            _focusOutline.Opacity = _keyboardFocusVisible ? 1 : 0;
        }
        BeginStateTransition();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        _keyboardFocusVisible = false;
        if (_focusOutline is not null) _focusOutline.Opacity = 0;
        BeginStateTransition();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Space or Key.Enter) BeginStateTransition(forcePressed: true);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.Space or Key.Enter) BeginStateTransition();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _blinkStartedAt = Stopwatch.GetTimestamp();
        SubscribeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsEnabled) BeginStateTransition();
        else ApplyStateImmediately();
    }

    private void BeginStateTransition(bool forcePressed = false)
    {
        var pressed = forcePressed || IsPressed;
        var targetAngle = !pressed && (IsMouseOver || _keyboardFocusVisible) ? -12.0 : 0.0;
        var targetColor = IsMouseOver ? _hoverCoverColor : _normalCoverColor;

        if (!IsEnabled)
        {
            ApplyState(targetAngle, targetColor);
            _transitionActive = false;
            return;
        }

        _transitionFromAngle = _currentAngle;
        _transitionToAngle = targetAngle;
        _transitionFromColor = _coverBrush?.Color ?? _normalCoverColor;
        _transitionToColor = targetColor;
        _useOvershootEase = targetAngle < _currentAngle;
        _transitionStartedAt = Stopwatch.GetTimestamp();
        _transitionActive = true;
        SubscribeRendering();
    }

    private void ApplyStateImmediately()
    {
        var targetAngle = IsEnabled && !IsPressed && (IsMouseOver || _keyboardFocusVisible) ? -12.0 : 0.0;
        var targetColor = IsEnabled && IsMouseOver ? _hoverCoverColor : _normalCoverColor;
        ApplyState(targetAngle, targetColor);
        _transitionActive = false;
    }

    private void ApplyState(double angle, Color color)
    {
        _currentAngle = angle;
        if (_coverRotation is not null) _coverRotation.Angle = angle;
        if (_coverBrush is not null) _coverBrush.Color = color;
    }

    private void UpdatePupils(Point pointer)
    {
        if (_eyes is null || ActualWidth <= 0) return;

        var eyeCenter = _eyes.TranslatePoint(new Point(_eyes.ActualWidth / 2, _eyes.ActualHeight / 2), this);
        var scale = Math.Max(ActualWidth / ReferenceWidth, 0.001);
        var offsetX = (pointer.X - eyeCenter.X) / (30.0 * scale);
        var offsetY = (pointer.Y - eyeCenter.Y) / (12.5 * scale);
        SetTranslation(_pupilOneTranslation, offsetX, offsetY);
        SetTranslation(_pupilTwoTranslation, offsetX, offsetY);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();

        if (_transitionActive)
        {
            var elapsed = (now - _transitionStartedAt) / (double)Stopwatch.Frequency;
            var phase = Math.Clamp(elapsed / TransitionDurationSeconds, 0, 1);
            var eased = _useOvershootEase
                ? CubicBezier(phase, 0.65, 0, 0.35, 1.65)
                : CubicBezier(phase, 0.65, 0, 0.35, 1);
            ApplyState(
                Lerp(_transitionFromAngle, _transitionToAngle, eased),
                Interpolate(_transitionFromColor, _transitionToColor, Math.Clamp(eased, 0, 1)));

            if (phase >= 1)
            {
                ApplyState(_transitionToAngle, _transitionToColor);
                _transitionActive = false;
            }
        }

        if (_blinkStartedAt != 0)
        {
            var seconds = (now - _blinkStartedAt) / (double)Stopwatch.Frequency;
            var phase = seconds / BlinkDurationSeconds;
            phase -= Math.Floor(phase);
            ApplyBlink(EvaluateBlink(phase));
        }
        else
        {
            ApplyBlink(1);
        }
    }

    private static double EvaluateBlink(double phase)
    {
        if (phase <= 0.92) return 1;
        if (phase <= 0.96)
        {
            var progress = (phase - 0.92) / 0.04;
            return 1 - CubicBezier(progress, 0.32, 0, 0.67, 0);
        }

        return CubicBezier((phase - 0.96) / 0.04, 0.33, 1, 0.68, 1);
    }

    private void ApplyBlink(double scaleY)
    {
        if (_eyeOneScale is not null) _eyeOneScale.ScaleY = scaleY;
        if (_eyeTwoScale is not null) _eyeTwoScale.ScaleY = scaleY;
    }

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

    private void HideKeyboardFocusVisual()
    {
        _keyboardFocusVisible = false;
        if (_focusOutline is not null) _focusOutline.Opacity = 0;
    }

    private static RotateTransform? InstallRotation(FrameworkElement? element, Point origin)
    {
        if (element is null) return null;
        var transform = new RotateTransform();
        element.RenderTransform = transform;
        element.RenderTransformOrigin = origin;
        return transform;
    }

    private static ScaleTransform? InstallScale(FrameworkElement? element, Point origin)
    {
        if (element is null) return null;
        var transform = new ScaleTransform(1, 1);
        element.RenderTransform = transform;
        element.RenderTransformOrigin = origin;
        return transform;
    }

    private static TranslateTransform? InstallTranslation(FrameworkElement? element)
    {
        if (element is null) return null;
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private static void SetTranslation(TranslateTransform? transform, double x, double y)
    {
        if (transform is null) return;
        transform.X = x;
        transform.Y = y;
    }

    private static Color GetBrushColor(Brush? brush) =>
        brush is SolidColorBrush solid ? solid.Color : default;

    private static Color Interpolate(Color from, Color to, double progress) => Color.FromArgb(
        Mix(from.A, to.A, progress),
        Mix(from.R, to.R, progress),
        Mix(from.G, to.G, progress),
        Mix(from.B, to.B, progress));

    private static byte Mix(byte from, byte to, double progress) =>
        (byte)Math.Round(Lerp(from, to, progress));

    private static double Lerp(double from, double to, double progress) => from + ((to - from) * progress);

    private static double CubicBezier(double x, double x1, double y1, double x2, double y2)
    {
        var parameter = Math.Clamp(x, 0, 1);
        for (var index = 0; index < 8; index++)
        {
            var error = Sample(parameter, x1, x2) - x;
            var derivative = SampleDerivative(parameter, x1, x2);
            if (Math.Abs(error) < 0.000001 || Math.Abs(derivative) < 0.000001) break;
            parameter = Math.Clamp(parameter - (error / derivative), 0, 1);
        }

        return Sample(parameter, y1, y2);
    }

    private static double Sample(double t, double first, double second)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * t * first) + (3 * inverse * t * t * second) + (t * t * t);
    }

    private static double SampleDerivative(double t, double first, double second)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * first) + (6 * inverse * t * (second - first)) + (3 * t * t * (1 - second));
    }
}
