using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CheemsUI;

/// <summary>带数值响应光晕与机械旋钮的可拖动进度条。</summary>
[TemplatePart(Name = PartTrackSurfaceName, Type = typeof(Canvas))]
public sealed class CheemsGlowSlider : CheemsDraggableProgressBar
{
    private const string PartTrackSurfaceName = "PartTrackSurface";
    private const string PartTrackName = "PartTrack";
    private const string PartFillGlowName = "PartFillGlow";
    private const string PartFillName = "PartFill";
    private const string PartThumbName = "PartThumb";
    private const string PartThumbGlowName = "PartThumbGlow";
    private const string PartThumbCoreName = "PartThumbCore";
    private const double ThumbSize = 64;
    private const double TrackInset = ThumbSize / 2;

    private Canvas? _trackSurface;
    private FrameworkElement? _track;
    private FrameworkElement? _fillGlow;
    private FrameworkElement? _fill;
    private FrameworkElement? _thumb;
    private FrameworkElement? _thumbGlow;
    private FrameworkElement? _thumbCore;
    private DropShadowEffect? _fillGlowEffect;
    private DropShadowEffect? _thumbGlowEffect;

    public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(
        nameof(GlowColor),
        typeof(Color),
        typeof(CheemsGlowSlider),
        new FrameworkPropertyMetadata(Color.FromRgb(31, 224, 160), OnGlowColorChanged));

    static CheemsGlowSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsGlowSlider),
            new FrameworkPropertyMetadata(typeof(CheemsGlowSlider)));
    }

    /// <summary>进度填充、旋钮核心及外发光所使用的颜色。</summary>
    public Color GlowColor
    {
        get => (Color)GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_trackSurface is not null)
        {
            _trackSurface.SizeChanged -= OnTrackSizeChanged;
        }

        base.OnApplyTemplate();
        _trackSurface = GetTemplateChild(PartTrackSurfaceName) as Canvas;
        _track = GetTemplateChild(PartTrackName) as FrameworkElement;
        _fillGlow = GetTemplateChild(PartFillGlowName) as FrameworkElement;
        _fill = GetTemplateChild(PartFillName) as FrameworkElement;
        _thumb = GetTemplateChild(PartThumbName) as FrameworkElement;
        _thumbGlow = GetTemplateChild(PartThumbGlowName) as FrameworkElement;
        _thumbCore = GetTemplateChild(PartThumbCoreName) as FrameworkElement;

        _fillGlowEffect = EnsureGlowEffect(_fillGlow);
        _thumbGlowEffect = EnsureGlowEffect(_thumbGlow);
        ApplyGlowColor();

        if (_trackSurface is not null)
        {
            _trackSurface.SizeChanged += OnTrackSizeChanged;
        }

        UpdateVisuals();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateVisuals();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MinimumProperty || e.Property == MaximumProperty)
        {
            UpdateVisuals();
        }
    }

    private static void OnGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CheemsGlowSlider)d).ApplyGlowColor();

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void UpdateVisuals()
    {
        if (_trackSurface is null || _trackSurface.ActualWidth <= 0)
        {
            return;
        }

        var range = Maximum - Minimum;
        var progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        var travel = Math.Max(0, _trackSurface.ActualWidth - (TrackInset * 2));
        var thumbCenter = TrackInset + (travel * progress);
        var fillWidth = Math.Max(8, travel * progress);

        PositionTrack(_track, travel);
        PositionFill(_fillGlow, fillWidth);
        PositionFill(_fill, fillWidth);
        if (_thumb is not null)
        {
            Canvas.SetLeft(_thumb, thumbCenter - (ThumbSize / 2));
        }

        var strength = 0.25 + (0.75 * progress);
        if (_fill is not null) _fill.Opacity = strength;
        if (_fillGlow is not null) _fillGlow.Opacity = 0.24 + (0.66 * progress);
        if (_thumbGlow is not null) _thumbGlow.Opacity = 0.12 + (0.72 * progress);
        if (_thumbCore is not null) _thumbCore.Opacity = 0.36 + (0.64 * progress);

        if (_fillGlowEffect is not null)
        {
            _fillGlowEffect.BlurRadius = 13 + (25 * progress);
            _fillGlowEffect.Opacity = 0.3 + (0.7 * progress);
        }
        if (_thumbGlowEffect is not null)
        {
            _thumbGlowEffect.BlurRadius = 7 + (20 * progress);
            _thumbGlowEffect.Opacity = 0.2 + (0.75 * progress);
        }
    }

    private void ApplyGlowColor()
    {
        var brush = new SolidColorBrush(GlowColor);
        brush.Freeze();
        if (_fill is Border fill) fill.SetCurrentValue(Border.BackgroundProperty, brush);
        if (_fillGlow is Border fillGlow) fillGlow.SetCurrentValue(Border.BackgroundProperty, brush);
        if (_thumbGlow is not null) _thumbGlow.SetCurrentValue(System.Windows.Shapes.Shape.FillProperty, brush);
        if (_thumbCore is not null) _thumbCore.SetCurrentValue(System.Windows.Shapes.Shape.FillProperty, brush);
        if (_fillGlowEffect is not null) _fillGlowEffect.Color = GlowColor;
        if (_thumbGlowEffect is not null) _thumbGlowEffect.Color = GlowColor;
    }

    private static DropShadowEffect? EnsureGlowEffect(FrameworkElement? element)
    {
        if (element is null) return null;
        var effect = new DropShadowEffect { Direction = 0, ShadowDepth = 0, BlurRadius = 16, Opacity = 0.6 };
        element.Effect = effect;
        return effect;
    }

    private static void PositionTrack(FrameworkElement? element, double width)
    {
        if (element is null) return;
        Canvas.SetLeft(element, TrackInset);
        element.Width = width;
    }

    private static void PositionFill(FrameworkElement? element, double width)
    {
        if (element is null) return;
        Canvas.SetLeft(element, TrackInset);
        element.Width = width;
    }
}
