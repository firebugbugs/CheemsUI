using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CheemsUI;

/// <summary>进度值文字的显示方式。</summary>
public enum CheemsProgressValueDisplayMode
{
    Percentage,
    Value
}

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

    public static readonly DependencyProperty ValueDisplayModeProperty = DependencyProperty.Register(
        nameof(ValueDisplayMode),
        typeof(CheemsProgressValueDisplayMode),
        typeof(CheemsDraggableProgressBar),
        new FrameworkPropertyMetadata(CheemsProgressValueDisplayMode.Percentage, OnValueTextOptionChanged));

    public static readonly DependencyProperty ValueStringFormatProperty = DependencyProperty.Register(
        nameof(ValueStringFormat),
        typeof(string),
        typeof(CheemsDraggableProgressBar),
        new FrameworkPropertyMetadata("0.##", OnValueTextOptionChanged),
        value => value is string);

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText),
        typeof(string),
        typeof(CheemsDraggableProgressBar),
        new FrameworkPropertyMetadata("0%"));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

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

    /// <summary>选择显示归一化百分比或 Value 的普通数字。</summary>
    public CheemsProgressValueDisplayMode ValueDisplayMode
    {
        get => (CheemsProgressValueDisplayMode)GetValue(ValueDisplayModeProperty);
        set => SetValue(ValueDisplayModeProperty, value);
    }

    /// <summary>数值格式，默认 0.##；百分比模式下格式化 0–100 的数值。</summary>
    public string ValueStringFormat
    {
        get => (string)GetValue(ValueStringFormatProperty);
        set => SetValue(ValueStringFormatProperty, value);
    }

    public string DisplayText => (string)GetValue(DisplayTextProperty);

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateDisplayText();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MinimumProperty || e.Property == MaximumProperty)
        {
            UpdateDisplayText();
        }
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

    private static void OnValueTextOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((CheemsDraggableProgressBar)dependencyObject).UpdateDisplayText();

    private void UpdateDisplayText()
    {
        var number = ValueDisplayMode == CheemsProgressValueDisplayMode.Value
            ? Value
            : (Maximum - Minimum) <= 0
                ? 0
                : Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) * 100;

        string formatted;
        try
        {
            formatted = number.ToString(ValueStringFormat, System.Globalization.CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            formatted = number.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        }

        SetValue(DisplayTextPropertyKey,
            ValueDisplayMode == CheemsProgressValueDisplayMode.Percentage ? $"{formatted}%" : formatted);
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
