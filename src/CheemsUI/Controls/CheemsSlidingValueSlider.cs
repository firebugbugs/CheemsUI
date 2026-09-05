using System.Windows;
using System.Windows.Controls;

namespace CheemsUI;

/// <summary>数值气泡随旋钮移动、数字在气泡中横向滑动的进度条。</summary>
[TemplatePart(Name = PartTrackSurfaceName, Type = typeof(Canvas))]
[TemplatePart(Name = PartPreviousValueName, Type = typeof(TextBlock))]
[TemplatePart(Name = PartNextValueName, Type = typeof(TextBlock))]
public sealed class CheemsSlidingValueSlider : CheemsDraggableProgressBar
{
    private const string PartTrackSurfaceName = "PartTrackSurface";
    private const string PartTrackName = "PartTrack";
    private const string PartFillName = "PartFill";
    private const string PartThumbName = "PartThumb";
    private const string PartOutputName = "PartOutput";
    private const string PartPreviousValueName = "PartPreviousValue";
    private const string PartNextValueName = "PartNextValue";
    private const double ThumbSize = 30;
    private const double TrackInset = ThumbSize / 2;
    private const double ValueCellWidth = 56;

    private Canvas? _trackSurface;
    private FrameworkElement? _track;
    private FrameworkElement? _fill;
    private FrameworkElement? _thumb;
    private FrameworkElement? _output;
    private TextBlock? _previousValue;
    private TextBlock? _nextValue;

    static CheemsSlidingValueSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsSlidingValueSlider),
            new FrameworkPropertyMetadata(typeof(CheemsSlidingValueSlider)));
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
        _fill = GetTemplateChild(PartFillName) as FrameworkElement;
        _thumb = GetTemplateChild(PartThumbName) as FrameworkElement;
        _output = GetTemplateChild(PartOutputName) as FrameworkElement;
        _previousValue = GetTemplateChild(PartPreviousValueName) as TextBlock;
        _nextValue = GetTemplateChild(PartNextValueName) as TextBlock;
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

        if (_track is not null)
        {
            Canvas.SetLeft(_track, TrackInset);
            _track.Width = travel;
        }
        if (_fill is not null)
        {
            Canvas.SetLeft(_fill, TrackInset);
            _fill.Width = travel * progress;
        }
        if (_thumb is not null)
        {
            Canvas.SetLeft(_thumb, thumbCenter - (ThumbSize / 2));
        }
        if (_output is not null)
        {
            Canvas.SetLeft(_output, thumbCenter - (ValueCellWidth / 2));
        }
        if (_previousValue is not null && _nextValue is not null)
        {
            var displayedValue = Math.Clamp(Value, Minimum, Maximum);
            var previous = Math.Floor(displayedValue);
            var next = Math.Min(Math.Ceiling(displayedValue), Maximum);
            if (next <= previous && displayedValue < Maximum)
            {
                next = previous + 1;
            }

            var fraction = displayedValue - previous;
            _previousValue.Text = previous.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
            _nextValue.Text = next.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
            Canvas.SetLeft(_previousValue, -fraction * ValueCellWidth);
            Canvas.SetLeft(_nextValue, (1 - fraction) * ValueCellWidth);
        }
    }
}
