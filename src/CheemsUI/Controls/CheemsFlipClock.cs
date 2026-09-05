using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheemsUI;

/// <summary>
/// Minimal Flip Clock 的 WPF 移植版。控件只负责显示和翻页动画，时间来源由 <see cref="Value"/> 绑定提供。
/// </summary>
[TemplatePart(Name = "PartHourBaseText", Type = typeof(TextBlock))]
[TemplatePart(Name = "PartMinuteBaseText", Type = typeof(TextBlock))]
[TemplatePart(Name = "PartSecondBaseText", Type = typeof(TextBlock))]
public sealed class CheemsFlipClock : Control
{
    private static readonly Duration HalfFlipDuration = new(TimeSpan.FromMilliseconds(300));
    private readonly FlipUnit _hour = new("Hour");
    private readonly FlipUnit _minute = new("Minute");
    private readonly FlipUnit _second = new("Second");
    private bool _templateReady;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(DateTime),
        typeof(CheemsFlipClock),
        new FrameworkPropertyMetadata(default(DateTime), OnValueChanged));

    public static readonly DependencyProperty CardBackgroundProperty = DependencyProperty.Register(
        nameof(CardBackground),
        typeof(Brush),
        typeof(CheemsFlipClock),
        new FrameworkPropertyMetadata(Brushes.White));

    static CheemsFlipClock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsFlipClock),
            new FrameworkPropertyMetadata(typeof(CheemsFlipClock)));
    }

    public DateTime Value
    {
        get => (DateTime)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Brush CardBackground
    {
        get => (Brush)GetValue(CardBackgroundProperty);
        set => SetValue(CardBackgroundProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _templateReady = _hour.Attach(this) & _minute.Attach(this) & _second.Attach(this);
        if (_templateReady) ApplyValue(Value, animate: false);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var clock = (CheemsFlipClock)d;
        if (clock._templateReady) clock.ApplyValue((DateTime)e.NewValue, animate: true);
    }

    private void ApplyValue(DateTime value, bool animate)
    {
        _hour.Set(value.Hour.ToString("00"), animate);
        _minute.Set(value.Minute.ToString("00"), animate);
        _second.Set(value.Second.ToString("00"), animate);
    }

    private sealed class FlipUnit
    {
        private readonly string _name;
        private TextBlock? _baseText;
        private TextBlock? _oldTopText;
        private TextBlock? _oldBottomText;
        private TextBlock? _newBottomText;
        private FrameworkElement? _oldTop;
        private FrameworkElement? _oldBottom;
        private FrameworkElement? _newBottom;
        private ScaleTransform? _oldTopScale;
        private ScaleTransform? _newBottomScale;
        private string? _value;
        private int _animationVersion;

        public FlipUnit(string name) => _name = name;

        public bool Attach(Control owner)
        {
            _baseText = owner.Template.FindName($"Part{_name}BaseText", owner) as TextBlock;
            _oldTopText = owner.Template.FindName($"Part{_name}OldTopText", owner) as TextBlock;
            _oldBottomText = owner.Template.FindName($"Part{_name}OldBottomText", owner) as TextBlock;
            _newBottomText = owner.Template.FindName($"Part{_name}NewBottomText", owner) as TextBlock;
            _oldTop = owner.Template.FindName($"Part{_name}OldTop", owner) as FrameworkElement;
            _oldBottom = owner.Template.FindName($"Part{_name}OldBottom", owner) as FrameworkElement;
            _newBottom = owner.Template.FindName($"Part{_name}NewBottom", owner) as FrameworkElement;
            // 模板中的 Freezable 可能被 WPF 冻结；为每个实例安装可动画的独立变换。
            _oldTopScale = InstallScale(_oldTop, 1);
            _newBottomScale = InstallScale(_newBottom, 0);
            _value = null;
            return _baseText is not null && _oldTopText is not null && _oldBottomText is not null &&
                   _newBottomText is not null && _oldTop is not null && _oldBottom is not null &&
                   _newBottom is not null && _oldTopScale is not null && _newBottomScale is not null;
        }

        private static ScaleTransform? InstallScale(FrameworkElement? element, double scaleY)
        {
            if (element is null) return null;
            var transform = new ScaleTransform(1, scaleY);
            element.RenderTransform = transform;
            return transform;
        }

        public void Set(string value, bool animate)
        {
            if (_baseText is null || _value == value) return;

            var previous = _value;
            _value = value;
            if (!animate || previous is null)
            {
                ShowImmediately(value);
                return;
            }

            BeginFlip(previous, value);
        }

        private void ShowImmediately(string value)
        {
            _animationVersion++;
            StopAnimations();
            _baseText!.Text = value;
            _oldTop!.Visibility = Visibility.Collapsed;
            _oldBottom!.Visibility = Visibility.Collapsed;
            _newBottom!.Visibility = Visibility.Collapsed;
        }

        private void BeginFlip(string previous, string next)
        {
            var version = ++_animationVersion;
            StopAnimations();

            _baseText!.Text = next;
            _oldTopText!.Text = previous;
            _oldBottomText!.Text = previous;
            _newBottomText!.Text = next;
            _oldTop!.Visibility = Visibility.Visible;
            _oldBottom!.Visibility = Visibility.Visible;
            _newBottom!.Visibility = Visibility.Visible;
            _oldTopScale!.ScaleY = 1;
            _newBottomScale!.ScaleY = 0;

            var topAnimation = new DoubleAnimation(1, 0, HalfFlipDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.HoldEnd
            };
            var bottomAnimation = new DoubleAnimation(0, 1, HalfFlipDuration)
            {
                BeginTime = HalfFlipDuration.TimeSpan,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };
            bottomAnimation.Completed += (_, _) =>
            {
                if (version != _animationVersion) return;
                StopAnimations();
                _oldTop.Visibility = Visibility.Collapsed;
                _oldBottom.Visibility = Visibility.Collapsed;
                _newBottom.Visibility = Visibility.Collapsed;
            };

            _oldTopScale.BeginAnimation(ScaleTransform.ScaleYProperty, topAnimation, HandoffBehavior.SnapshotAndReplace);
            _newBottomScale.BeginAnimation(ScaleTransform.ScaleYProperty, bottomAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private void StopAnimations()
        {
            _oldTopScale?.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _newBottomScale?.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            if (_oldTopScale is not null) _oldTopScale.ScaleY = 1;
            if (_newBottomScale is not null) _newBottomScale.ScaleY = 0;
        }
    }
}
