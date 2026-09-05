using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CheemsUI;

/// <summary>CodePen alvaromontoro Flight Slider 的可拖动 WPF 等价实现。</summary>
[TemplatePart(Name = PartTrackSurfaceName, Type = typeof(Canvas))]
[TemplatePart(Name = PartProgressFillName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPlaneName, Type = typeof(FrameworkElement))]
public sealed class CheemsFlightProgressBar : CheemsDraggableProgressBar
{
    private const string PartTrackSurfaceName = "PartTrackSurface";
    private const string PartProgressFillName = "PartProgressFill";
    private const string PartBaseTrackName = "PartBaseTrack";
    private const string PartStartDotName = "PartStartDot";
    private const string PartEndDotName = "PartEndDot";
    private const string PartCompletedStartDotName = "PartCompletedStartDot";
    private const string PartPlaneName = "PartPlane";
    private const string PartCloudOneName = "PartCloudOne";
    private const string PartCloudTwoName = "PartCloudTwo";
    private const double PlaneSize = 64;
    private const double TrackInset = PlaneSize / 2;

    private Canvas? _trackSurface;
    private FrameworkElement? _progressFill;
    private FrameworkElement? _baseTrack;
    private FrameworkElement? _startDot;
    private FrameworkElement? _endDot;
    private FrameworkElement? _completedStartDot;
    private FrameworkElement? _plane;
    private ScaleTransform? _planeScale;
    private DropShadowEffect? _planeShadow;
    private TranslateTransform? _cloudOneTranslation;
    private TranslateTransform? _cloudTwoTranslation;

    public static readonly DependencyProperty OriginProperty = DependencyProperty.Register(
        nameof(Origin), typeof(string), typeof(CheemsFlightProgressBar), new FrameworkPropertyMetadata("New York"));

    public static readonly DependencyProperty DestinationProperty = DependencyProperty.Register(
        nameof(Destination), typeof(string), typeof(CheemsFlightProgressBar), new FrameworkPropertyMetadata("Madrid"));

    static CheemsFlightProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsFlightProgressBar),
            new FrameworkPropertyMetadata(typeof(CheemsFlightProgressBar)));
    }

    public string Origin { get => (string)GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public string Destination { get => (string)GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }

    public override void OnApplyTemplate()
    {
        if (_trackSurface is not null) _trackSurface.SizeChanged -= OnTrackSizeChanged;
        base.OnApplyTemplate();

        _trackSurface = GetTemplateChild(PartTrackSurfaceName) as Canvas;
        _progressFill = GetTemplateChild(PartProgressFillName) as FrameworkElement;
        _baseTrack = GetTemplateChild(PartBaseTrackName) as FrameworkElement;
        _startDot = GetTemplateChild(PartStartDotName) as FrameworkElement;
        _endDot = GetTemplateChild(PartEndDotName) as FrameworkElement;
        _completedStartDot = GetTemplateChild(PartCompletedStartDotName) as FrameworkElement;
        _plane = GetTemplateChild(PartPlaneName) as FrameworkElement;
        _planeScale = InstallScale(_plane);
        _planeShadow = InstallShadow(_plane);
        _cloudOneTranslation = InstallTranslation(GetTemplateChild(PartCloudOneName) as FrameworkElement);
        _cloudTwoTranslation = InstallTranslation(GetTemplateChild(PartCloudTwoName) as FrameworkElement);

        if (_trackSurface is not null) _trackSurface.SizeChanged += OnTrackSizeChanged;
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
        if (e.Property == MinimumProperty || e.Property == MaximumProperty) UpdateVisuals();
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void UpdateVisuals()
    {
        if (_trackSurface is null || _trackSurface.ActualWidth <= 0) return;
        var range = Maximum - Minimum;
        var progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        var travel = Math.Max(0, _trackSurface.ActualWidth - TrackInset * 2);
        var planeCenter = TrackInset + travel * progress;

        if (_baseTrack is not null)
        {
            Canvas.SetLeft(_baseTrack, TrackInset);
            _baseTrack.Width = travel;
        }
        if (_startDot is not null) Canvas.SetLeft(_startDot, TrackInset - 6);
        if (_endDot is not null) Canvas.SetLeft(_endDot, Math.Max(0, _trackSurface.ActualWidth - TrackInset - 6));
        if (_completedStartDot is not null) Canvas.SetLeft(_completedStartDot, TrackInset - 5);
        if (_progressFill is not null) Canvas.SetLeft(_progressFill, TrackInset);
        if (_progressFill is not null) _progressFill.Width = travel * progress;
        if (_plane is not null) Canvas.SetLeft(_plane, planeCenter - PlaneSize / 2);

        var lift = Math.Sin(Math.PI * progress);
        if (_planeScale is not null)
        {
            var scale = 1 + 0.5 * lift;
            _planeScale.ScaleX = scale;
            _planeScale.ScaleY = scale;
        }
        if (_planeShadow is not null)
        {
            _planeShadow.BlurRadius = 4 + 10 * lift;
            _planeShadow.ShadowDepth = 3 + 8 * lift;
            _planeShadow.Opacity = 0.4 - 0.2 * lift;
        }

        if (_cloudOneTranslation is not null) _cloudOneTranslation.X = (progress - 0.5) * travel * 0.45;
        if (_cloudTwoTranslation is not null) _cloudTwoTranslation.X = (0.5 - progress) * travel * 0.35;
    }

    private static ScaleTransform? InstallScale(FrameworkElement? element)
    {
        if (element is null) return null;
        var transform = new ScaleTransform(1, 1);
        element.RenderTransform = transform;
        return transform;
    }

    private static TranslateTransform? InstallTranslation(FrameworkElement? element)
    {
        if (element is null) return null;
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        return transform;
    }

    private static DropShadowEffect? InstallShadow(FrameworkElement? element)
    {
        if (element is null) return null;
        var effect = new DropShadowEffect { Color = Colors.Black, Direction = 315, BlurRadius = 4, ShadowDepth = 3, Opacity = 0.4 };
        element.Effect = effect;
        return effect;
    }
}
