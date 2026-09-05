using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheemsUI;

/// <summary>
/// A circular, multi-position switch whose positions come from <see cref="ItemsControl.ItemsSource"/>.
/// </summary>
[TemplatePart(Name = PartRootName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartLightName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartDotName, Type = typeof(FrameworkElement))]
public sealed class CheemsRotarySwitch : ListBox
{
    private const string PartRootName = "PartRoot";
    private const string PartLightName = "PartLight";
    private const string PartDotName = "PartDot";
    private const double StartAngle = -90.0;

    private FrameworkElement? _root;
    private FrameworkElement? _dot;
    private RotateTransform? _lightRotation;
    private RotateTransform? _dotRotation;

    static CheemsRotarySwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsRotarySwitch),
            new FrameworkPropertyMetadata(typeof(CheemsRotarySwitch)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _root = GetTemplateChild(PartRootName) as FrameworkElement;
        var light = GetTemplateChild(PartLightName) as FrameworkElement;
        _lightRotation = CreateInstanceRotation(light);
        _dot = GetTemplateChild(PartDotName) as FrameworkElement;
        _dotRotation = CreateInstanceRotation(_dot);
        UpdateIndicators(animate: false);
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        if (Items.Count > 0 && (SelectedIndex < 0 || SelectedIndex >= Items.Count))
        {
            SelectedIndex = 0;
        }

        UpdateIndicators(animate: false);
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        UpdateIndicators(animate: true);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (IsEnabled && _root is not null && Items.Count > 0)
        {
            var point = e.GetPosition(_root);
            var center = new Point(_root.ActualWidth / 2.0, _root.ActualHeight / 2.0);
            var x = point.X - center.X;
            var y = point.Y - center.Y;
            var radius = Math.Sqrt((x * x) + (y * y));

            // The original HTML labels occupy the annulus outside the 100 px centre knob.
            if (radius >= _root.ActualWidth * (50.0 / 230.0))
            {
                var angle = Math.Atan2(y, x) * 180.0 / Math.PI;
                var step = 360.0 / Items.Count;
                var clockwiseFromTop = NormalizeAngle(angle - StartAngle);
                SelectedIndex = (int)Math.Round(clockwiseFromTop / step, MidpointRounding.AwayFromZero) % Items.Count;
                Focus();
                e.Handled = true;
            }
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Items.Count > 0)
        {
            var nextIndex = SelectedIndex;
            switch (e.Key)
            {
                case Key.Right:
                case Key.Down:
                    nextIndex = (Math.Max(SelectedIndex, 0) + 1) % Items.Count;
                    break;
                case Key.Left:
                case Key.Up:
                    nextIndex = (SelectedIndex <= 0 ? Items.Count : SelectedIndex) - 1;
                    break;
                case Key.Home:
                    nextIndex = 0;
                    break;
                case Key.End:
                    nextIndex = Items.Count - 1;
                    break;
            }

            if (nextIndex != SelectedIndex)
            {
                SelectedIndex = nextIndex;
                e.Handled = true;
            }
        }

        base.OnKeyDown(e);
    }

    private void UpdateIndicators(bool animate)
    {
        if (Items.Count <= 0 || SelectedIndex < 0)
        {
            return;
        }

        var target = StartAngle + (SelectedIndex * 360.0 / Items.Count);
        SetAngle(_lightRotation, target, animate);
        SetAngle(_dotRotation, target, animate);
        SetOpacity(_dot, GetDotOpacity(SelectedIndex, Items.Count), animate);
    }

    private static void SetAngle(RotateTransform? transform, double target, bool animate)
    {
        if (transform is null)
        {
            return;
        }

        var current = transform.Angle;
        transform.BeginAnimation(RotateTransform.AngleProperty, null);
        transform.Angle = target;

        if (!animate || !SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(0.5),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(current, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(
            target,
            KeyTime.FromPercent(1),
            new KeySpline(0.25, 0.1, 0.25, 1.0)));
        transform.BeginAnimation(RotateTransform.AngleProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static double NormalizeAngle(double angle)
    {
        angle %= 360.0;
        return angle < 0 ? angle + 360.0 : angle;
    }

    private static RotateTransform? CreateInstanceRotation(FrameworkElement? element)
    {
        if (element?.RenderTransform is not RotateTransform templateRotation)
        {
            return null;
        }

        var instanceRotation = templateRotation.CloneCurrentValue();
        element.RenderTransform = instanceRotation;
        return instanceRotation;
    }

    private static void SetOpacity(FrameworkElement? element, double target, bool animate)
    {
        if (element is null)
        {
            return;
        }

        var current = element.Opacity;
        element.BeginAnimation(OpacityProperty, null);
        element.Opacity = target;
        if (animate && SystemParameters.ClientAreaAnimation)
        {
            element.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(current, target, TimeSpan.FromSeconds(0.5))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.Stop
                },
                HandoffBehavior.SnapshotAndReplace);
        }
    }

    private static double GetDotOpacity(int index, int count)
    {
        if (count == 6)
        {
            return new[] { 1.0, 0.9, 0.5, 0.4, 0.5, 0.9 }[index];
        }

        // Generalises the source's symmetric 1 → .4 → 1 falloff to any position count.
        return 0.7 + (0.3 * Math.Cos(index * Math.PI * 2.0 / count));
    }
}

/// <summary>Arranges switch labels around the original 220 px dial.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CheemsRotaryPositionsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(40, 70));
        }

        return new Size(
            double.IsInfinity(availableSize.Width) ? 220 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 220 : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = InternalChildren.Count;
        if (count == 0)
        {
            return finalSize;
        }

        var center = new Point(finalSize.Width / 2.0, finalSize.Height / 2.0);
        var radius = Math.Min(finalSize.Width, finalSize.Height) * (90.0 / 220.0);
        for (var index = 0; index < count; index++)
        {
            var radians = (-90.0 + (index * 360.0 / count)) * Math.PI / 180.0;
            var x = center.X + (Math.Cos(radians) * radius) - 20.0;
            var y = center.Y + (Math.Sin(radians) * radius) - 35.0;
            InternalChildren[index].Arrange(new Rect(x, y, 40, 70));
        }

        return finalSize;
    }
}

/// <summary>Draws dynamically counted two-tone separator rays behind the labels.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CheemsRotarySpokes : FrameworkElement
{
    public static readonly DependencyProperty PositionCountProperty = DependencyProperty.Register(
        nameof(PositionCount), typeof(int), typeof(CheemsRotarySpokes),
        new FrameworkPropertyMetadata(6, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DarkStrokeProperty = DependencyProperty.Register(
        nameof(DarkStroke), typeof(Brush), typeof(CheemsRotarySpokes),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LightStrokeProperty = DependencyProperty.Register(
        nameof(LightStroke), typeof(Brush), typeof(CheemsRotarySpokes),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public int PositionCount
    {
        get => (int)GetValue(PositionCountProperty);
        set => SetValue(PositionCountProperty, value);
    }

    public Brush DarkStroke
    {
        get => (Brush)GetValue(DarkStrokeProperty);
        set => SetValue(DarkStrokeProperty, value);
    }

    public Brush LightStroke
    {
        get => (Brush)GetValue(LightStrokeProperty);
        set => SetValue(LightStrokeProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (PositionCount <= 0 || DarkStroke is null || LightStroke is null)
        {
            return;
        }

        var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
        var radius = Math.Min(ActualWidth, ActualHeight) / 2.0;
        var darkPen = new Pen(DarkStroke, 1.0);
        var lightPen = new Pen(LightStroke, 1.0);

        for (var index = 0; index < PositionCount; index++)
        {
            var angle = -90.0 + ((index + 0.5) * 360.0 / PositionCount);
            var radians = angle * Math.PI / 180.0;
            var direction = new Vector(Math.Cos(radians), Math.Sin(radians));
            var normal = new Vector(-direction.Y, direction.X);
            var edge = center + (direction * radius);
            drawingContext.DrawLine(darkPen, center, edge);
            drawingContext.DrawLine(lightPen, center + normal, edge + normal);
        }
    }
}
