using System.Windows;
using System.Windows.Controls;

namespace CheemsUI;

/// <summary>
/// Uiverse thekuntal49 黑白进度条的可拖动 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartProgressFillName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartProgressTextName, Type = typeof(TextBlock))]
public sealed class CheemsMonoProgressBar : CheemsDraggableProgressBar
{
    private const string PartProgressFillName = "PartProgressFill";
    private const string PartProgressTextName = "PartProgressText";

    private FrameworkElement? _progressFill;

    static CheemsMonoProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsMonoProgressBar),
            new FrameworkPropertyMetadata(typeof(CheemsMonoProgressBar)));
    }

    public override void OnApplyTemplate()
    {
        if (DragSurface is not null)
        {
            DragSurface.SizeChanged -= OnDragSurfaceSizeChanged;
        }

        base.OnApplyTemplate();
        _progressFill = GetTemplateChild(PartProgressFillName) as FrameworkElement;

        if (DragSurface is not null)
        {
            DragSurface.SizeChanged += OnDragSurfaceSizeChanged;
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

    private void OnDragSurfaceSizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void UpdateVisuals()
    {
        var range = Maximum - Minimum;
        var progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);

        if (_progressFill is not null && DragSurface is not null)
        {
            _progressFill.Width = DragSurface.ActualWidth * progress;
        }

    }
}
