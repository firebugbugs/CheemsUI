using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse rust_1966 星空粒子进度条的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartViewportName, Type = typeof(Canvas))]
[TemplatePart(Name = PartProgressGroupName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartProgressTextName, Type = typeof(TextBlock))]
public sealed class CheemsCosmicProgressBar : CheemsDraggableProgressBar
{
    private const string PartViewportName = PartDragSurfaceName;
    private const string PartProgressGroupName = "PartProgressGroup";
    private const string PartProgressTextName = "PartProgressText";
    private static readonly string[] ParticleNames =
    {
        "PartParticleOne", "PartParticleTwo", "PartParticleThree", "PartParticleFour", "PartParticleFive"
    };

    private static readonly double[] ParticleLeft = { 0.20, 0.70, 0.50, 0.40, 0.60 };
    private static readonly double[] ParticleTop = { 0.10, 0.30, 0.50, 0.80, 0.90 };

    private readonly FrameworkElement?[] _particles = new FrameworkElement?[5];
    private Canvas? _viewport;
    private FrameworkElement? _progressGroup;
    private TextBlock? _progressText;
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
        _progressText = GetTemplateChild(PartProgressTextName) as TextBlock;
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

        if (_progressGroup is not null)
        {
            _progressGroup.Width = width * CalculateProgress();
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

        if (_progressText is not null)
        {
            _progressText.Text = $"{CalculateProgress() * 100:0.#}%";
        }
    }

    private double CalculateProgress()
    {
        var range = Maximum - Minimum;
        return range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
    }

}
