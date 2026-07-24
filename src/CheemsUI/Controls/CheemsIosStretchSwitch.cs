using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CheemsUI;

/// <summary>
/// Uiverse sayborduu iOS 按压延展开关的 WPF 等价实现。
/// </summary>
/// <remarks>
/// 本体为 51×31，拨块为 27×27；按住时拨块宽度扩展至 37，所有状态变化为 0.2 秒 ease-out。
/// </remarks>
public sealed class CheemsIosStretchSwitch : ToggleButton
{
    private const string PartThumbHostName = "PartThumbHost";
    private const double DurationSeconds = 0.2;
    private FrameworkElement? _thumbHost;

    static CheemsIosStretchSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsIosStretchSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsIosStretchSwitch)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _thumbHost = GetTemplateChild(PartThumbHostName) as FrameworkElement;
        ApplyThumbGeometry(animate: false);
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        ApplyThumbGeometry(animate: true);
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        ApplyThumbGeometry(animate: true);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        ApplyThumbGeometry(animate: true, pressedOverride: true);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ApplyThumbGeometry(animate: true, pressedOverride: false);
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        ApplyThumbGeometry(animate: true, pressedOverride: false);
    }

    private void ApplyThumbGeometry(bool animate, bool? pressedOverride = null)
    {
        if (_thumbHost is null)
        {
            return;
        }

        var isPressed = pressedOverride ?? IsPressed;
        var targetWidth = isPressed ? 37d : 27d;
        var targetLeft = IsChecked == true
            ? (isPressed ? 11.8 : 22d)
            : 2d;

        if (!animate || !SystemParameters.ClientAreaAnimation)
        {
            _thumbHost.BeginAnimation(WidthProperty, null);
            _thumbHost.BeginAnimation(Canvas.LeftProperty, null);
            _thumbHost.Width = targetWidth;
            Canvas.SetLeft(_thumbHost, targetLeft);
            return;
        }

        _thumbHost.BeginAnimation(WidthProperty, CreateEaseOutAnimation(targetWidth), HandoffBehavior.SnapshotAndReplace);
        _thumbHost.BeginAnimation(Canvas.LeftProperty, CreateEaseOutAnimation(targetLeft), HandoffBehavior.SnapshotAndReplace);
    }

    private static DoubleAnimationUsingKeyFrames CreateEaseOutAnimation(double target)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(DurationSeconds),
            FillBehavior = FillBehavior.HoldEnd
        };
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(
            target,
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(DurationSeconds)),
            new KeySpline(0, 0, 0.58, 1)));
        return animation;
    }
}
