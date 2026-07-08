using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CheemsControl;

/// <summary>
/// 进度页可拖动进度条的公共交互基类。单击不定位，超过系统拖拽阈值后才修改值。
/// </summary>
[TemplatePart(Name = PartDragSurfaceName, Type = typeof(FrameworkElement))]
public abstract class CheemsDraggableProgressBar : ProgressBar
{
    protected const string PartDragSurfaceName = "PartDragSurface";

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(CheemsDraggableProgressBar),
        new FrameworkPropertyMetadata(0.1),
        value => value is double step && double.IsFinite(step) && step > 0);

    private bool _dragArmed;
    private bool _isDragging;
    private double _dragStartX;
    private double _dragStartValue;

    protected FrameworkElement? DragSurface { get; private set; }

    /// <summary>
    /// 拖动时的数值步进，默认 0.1。
    /// </summary>
    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public override void OnApplyTemplate()
    {
        EndDrag();
        base.OnApplyTemplate();
        DragSurface = GetTemplateChild(PartDragSurfaceName) as FrameworkElement;
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (!IsEnabled || DragSurface is null || DragSurface.ActualWidth <= 0)
        {
            return;
        }

        var position = e.GetPosition(DragSurface);
        if (position.X < 0 || position.X > DragSurface.ActualWidth
            || position.Y < 0 || position.Y > DragSurface.ActualHeight)
        {
            return;
        }

        _dragArmed = true;
        _isDragging = false;
        _dragStartX = position.X;
        _dragStartValue = Value;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (!_dragArmed || DragSurface is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var horizontalDelta = e.GetPosition(DragSurface).X - _dragStartX;
        if (!_isDragging && Math.Abs(horizontalDelta) < SystemParameters.MinimumHorizontalDragDistance)
        {
            return;
        }

        _isDragging = true;
        var range = Maximum - Minimum;
        if (range > 0 && DragSurface.ActualWidth > 0)
        {
            var rawValue = _dragStartValue + ((horizontalDelta / DragSurface.ActualWidth) * range);
            SetCurrentValue(ValueProperty, SnapToStep(rawValue));
        }

        e.Handled = true;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!_dragArmed)
        {
            return;
        }

        EndDrag();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        _dragArmed = false;
        _isDragging = false;
        base.OnLostMouseCapture(e);
    }

    private double SnapToStep(double value)
    {
        var steps = Math.Round((value - Minimum) / Step, MidpointRounding.AwayFromZero);
        return Math.Clamp(Minimum + (steps * Step), Minimum, Maximum);
    }

    private void EndDrag()
    {
        _dragArmed = false;
        _isDragging = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }
}
