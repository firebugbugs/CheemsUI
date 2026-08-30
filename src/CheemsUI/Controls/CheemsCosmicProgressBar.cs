using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheemsUI;

/// <summary>
/// Uiverse rust_1966 星空粒子进度条的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartViewportName, Type = typeof(Canvas))]
[TemplatePart(Name = PartProgressGroupName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartProgressTextName, Type = typeof(TextBlock))]
[TemplatePart(Name = PartSurfaceName, Type = typeof(CheemsCosmicProgressSurface))]
public sealed class CheemsCosmicProgressBar : CheemsDraggableProgressBar
{
    private const string PartViewportName = PartDragSurfaceName;
    private const string PartProgressGroupName = "PartProgressGroup";
    private const string PartProgressTextName = "PartProgressText";
    private const string PartRippleScaleHostName = "PartRippleScaleHost";
    private const string PartSurfaceName = "PartSurface";
    private static readonly string[] ParticleNames =
    {
        "PartParticleOne", "PartParticleTwo", "PartParticleThree", "PartParticleFour", "PartParticleFive"
    };

    private static readonly double[] ParticleLeft = { 0.20, 0.70, 0.50, 0.40, 0.60 };
    private static readonly double[] ParticleTop = { 0.10, 0.30, 0.50, 0.80, 0.90 };
    private static readonly double[] ParticleBeginSeconds = { 0, 1, 2, 1.5, 2.5 };

    private readonly FrameworkElement?[] _particles = new FrameworkElement?[5];
    private Canvas? _viewport;
    private FrameworkElement? _progressGroup;
    private CheemsCosmicProgressSurface? _surface;
    private RectangleGeometry? _viewportClip;

    static CheemsCosmicProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCosmicProgressBar),
            new FrameworkPropertyMetadata(typeof(CheemsCosmicProgressBar)));
    }

    public override void OnApplyTemplate()
    {
        if (_viewport is not null)
        {
            _viewport.SizeChanged -= OnViewportSizeChanged;
        }

        base.OnApplyTemplate();
        _viewport = GetTemplateChild(PartViewportName) as Canvas;
        _progressGroup = GetTemplateChild(PartProgressGroupName) as FrameworkElement;
        _surface = GetTemplateChild(PartSurfaceName) as CheemsCosmicProgressSurface;
        for (var index = 0; index < ParticleNames.Length; index++)
        {
            _particles[index] = GetTemplateChild(ParticleNames[index]) as FrameworkElement;
        }

        if (_viewport is not null)
        {
            _viewportClip = new RectangleGeometry();
            _viewport.Clip = _viewportClip;
            _viewport.SizeChanged += OnViewportSizeChanged;
        }

        UpdateVisuals();
        StartTemplateAnimations();
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

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void UpdateVisuals()
    {
        if (_viewport is null)
        {
            return;
        }

        var width = Math.Max(0, _viewport.ActualWidth);
        var height = Math.Max(0, _viewport.ActualHeight);
        if (_viewportClip is not null)
        {
            _viewportClip.Rect = new Rect(0, 0, width, height);
            _viewportClip.RadiusX = height / 2;
            _viewportClip.RadiusY = height / 2;
        }

        var progress = CalculateProgress();
        if (_surface is not null)
        {
            _surface.Progress = progress;
        }

        if (_progressGroup is not null)
        {
            Canvas.SetLeft(_progressGroup, 0);
            Canvas.SetTop(_progressGroup, 0);
            _progressGroup.Height = height;
            _progressGroup.Width = width * progress;
        }

        for (var index = 0; index < _particles.Length; index++)
        {
            if (_particles[index] is not FrameworkElement particle)
            {
                continue;
            }

            Canvas.SetLeft(particle, width * ParticleLeft[index]);
            Canvas.SetTop(particle, height * ParticleTop[index]);
        }

    }

    private double CalculateProgress()
    {
        var range = Maximum - Minimum;
        return range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
    }

    private void StartTemplateAnimations()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        if (GetTemplateChild(PartRippleScaleHostName) is FrameworkElement rippleHost)
        {
            // 主题字典编译时会冻结模板内的 Freezable 并跨实例共享，不能直接动画，需换成可变实例。
            var rippleScale = new ScaleTransform(0.5, 0.5);
            rippleHost.RenderTransform = rippleScale;
            var duration = TimeSpan.FromSeconds(3);
            var repeat = RepeatBehavior.Forever;
            rippleScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.5, 1.5, duration) { RepeatBehavior = repeat });
            rippleScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.5, 1.5, duration) { RepeatBehavior = repeat });
            rippleHost.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0.7, 0, duration) { RepeatBehavior = repeat });
        }

        for (var index = 0; index < _particles.Length; index++)
        {
            if (_particles[index] is not FrameworkElement particle)
            {
                continue;
            }

            var transform = new TranslateTransform();
            particle.RenderTransform = transform;
            var beginTime = TimeSpan.FromSeconds(ParticleBeginSeconds[index]);
            transform.BeginAnimation(TranslateTransform.XProperty, CreateParticleAnimation(10, beginTime));
            transform.BeginAnimation(TranslateTransform.YProperty, CreateParticleAnimation(-20, beginTime));
        }
    }

    private static DoubleAnimationUsingKeyFrames CreateParticleAnimation(double midpoint, TimeSpan beginTime)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            BeginTime = beginTime,
            Duration = TimeSpan.FromSeconds(5),
            RepeatBehavior = RepeatBehavior.Forever
        };
        var easing = new KeySpline(0.42, 0, 0.58, 1);
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(
            midpoint, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.5)), easing));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(
            0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(5)), easing));
        return animation;
    }

}
