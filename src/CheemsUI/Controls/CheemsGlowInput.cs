using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse reglobby input 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartGlowName, Type = typeof(Border))]
[TemplatePart(Name = PartSurfaceName, Type = typeof(Border))]
[TemplatePart(Name = PartPlaceholderName, Type = typeof(TextBlock))]
public sealed class CheemsGlowInput : TextBox
{
    private const string PartGlowName = "PartGlow";
    private const string PartSurfaceName = "PartSurface";
    private const string PartPlaceholderName = "PartPlaceholder";
    private const double TransitionDurationSeconds = 0.5;
    private const double GlowSpread = 7;
    private const double BaseCornerRadius = 10;

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder),
        typeof(string),
        typeof(CheemsGlowInput),
        new FrameworkPropertyMetadata(string.Empty));

    private Border? _glow;
    private TextBlock? _placeholder;
    private SolidColorBrush? _surfaceBackground;
    private SolidColorBrush? _surfaceBorder;
    private Color _normalBackgroundColor;
    private Color _activeBackgroundColor;
    private Color _normalBorderColor;
    private Color _activeBorderColor;
    private double _progress;
    private double _transitionStartProgress;
    private double _transitionTarget;
    private double _transitionDuration;
    private long _transitionStartedAt;
    private bool _renderingSubscribed;

    static CheemsGlowInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsGlowInput),
            new FrameworkPropertyMetadata(typeof(CheemsGlowInput)));
    }

    public CheemsGlowInput()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _glow = GetTemplateChild(PartGlowName) as Border;
        _placeholder = GetTemplateChild(PartPlaceholderName) as TextBlock;
        if (GetTemplateChild(PartSurfaceName) is Border surface)
        {
            _normalBackgroundColor = FindColor(CheemsKeys.GlowInputBackgroundColor);
            _activeBackgroundColor = FindColor(CheemsKeys.GlowInputActiveBackgroundColor);
            _normalBorderColor = FindColor(CheemsKeys.GlowInputTransparentBorderColor);
            _activeBorderColor = FindColor(CheemsKeys.GlowInputAccentColor);

            _surfaceBackground = new SolidColorBrush(_normalBackgroundColor);
            _surfaceBorder = new SolidColorBrush(_normalBorderColor);
            surface.Background = _surfaceBackground;
            surface.BorderBrush = _surfaceBorder;
        }

        ApplyFrame(_progress);
        UpdatePlaceholderVisibility();
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        UpdatePlaceholderVisibility();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        UpdateInteractiveState();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        UpdateInteractiveState();
    }

    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusWithinChanged(e);
        UpdateInteractiveState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var target = IsMouseOver || IsKeyboardFocusWithin ? 1.0 : 0.0;
        _progress = target;
        _transitionTarget = target;
        ApplyFrame(_progress);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void UpdateInteractiveState()
    {
        var target = IsMouseOver || IsKeyboardFocusWithin ? 1.0 : 0.0;
        BeginTransition(target);
    }

    private void BeginTransition(double target)
    {
        var now = Stopwatch.GetTimestamp();
        UpdateProgress(now);

        if (Math.Abs(target - _transitionTarget) < 0.0001 && _renderingSubscribed)
        {
            return;
        }

        _transitionStartProgress = _progress;
        _transitionTarget = target;
        _transitionDuration = TransitionDurationSeconds * Math.Abs(target - _transitionStartProgress);
        _transitionStartedAt = now;

        if (!SystemParameters.ClientAreaAnimation || _transitionDuration < 0.0001)
        {
            _progress = target;
            ApplyFrame(_progress);
            UnsubscribeRendering();
            return;
        }

        SubscribeRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (UpdateProgress(Stopwatch.GetTimestamp()))
        {
            UnsubscribeRendering();
        }

        ApplyFrame(_progress);
    }

    private bool UpdateProgress(long now)
    {
        if (_transitionStartedAt == 0 || _transitionDuration <= 0)
        {
            return true;
        }

        var elapsed = (now - _transitionStartedAt) / (double)Stopwatch.Frequency;
        var linearProgress = Math.Clamp(elapsed / _transitionDuration, 0, 1);
        var easedProgress = CssEase(linearProgress);
        _progress = Lerp(_transitionStartProgress, _transitionTarget, easedProgress);
        return linearProgress >= 1;
    }

    private void ApplyFrame(double progress)
    {
        if (_surfaceBackground is not null)
        {
            _surfaceBackground.Color = Mix(_normalBackgroundColor, _activeBackgroundColor, progress);
        }

        if (_surfaceBorder is not null)
        {
            _surfaceBorder.Color = Mix(_normalBorderColor, _activeBorderColor, progress);
        }

        if (_glow is not null)
        {
            var spread = GlowSpread * progress;
            _glow.Margin = new Thickness(-spread);
            _glow.CornerRadius = new CornerRadius(BaseCornerRadius + spread);
            _glow.Opacity = progress;
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        if (_placeholder is not null)
        {
            _placeholder.Visibility = string.IsNullOrEmpty(Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private Color FindColor(string key)
    {
        return FindResource(key) switch
        {
            Color color => color,
            SolidColorBrush brush => brush.Color,
            _ => throw new InvalidOperationException($"资源 {key} 不是 Color 或 SolidColorBrush。")
        };
    }

    // CSS transition-timing-function: ease = cubic-bezier(.25,.1,.25,1).
    private static double CssEase(double x)
    {
        var lower = 0.0;
        var upper = 1.0;
        var t = x;

        for (var index = 0; index < 12; index++)
        {
            t = (lower + upper) / 2;
            if (CubicBezier(t, 0.25, 0.25) < x)
            {
                lower = t;
            }
            else
            {
                upper = t;
            }
        }

        return CubicBezier(t, 0.1, 1);
    }

    private static double CubicBezier(double t, double control1, double control2)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * t * control1)
               + (3 * inverse * t * t * control2)
               + (t * t * t);
    }

    private static double Lerp(double start, double end, double progress) =>
        start + ((end - start) * progress);

    private static Color Mix(Color start, Color end, double progress) => Color.FromArgb(
        (byte)Math.Round(Lerp(start.A, end.A, progress)),
        (byte)Math.Round(Lerp(start.R, end.R, progress)),
        (byte)Math.Round(Lerp(start.G, end.G, progress)),
        (byte)Math.Round(Lerp(start.B, end.B, progress)));

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
}
