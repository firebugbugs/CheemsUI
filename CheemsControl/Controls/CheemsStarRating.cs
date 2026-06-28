using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheemsControl;

/// <summary>
/// Uiverse PriyanshuGupta28 五星 RadioButton 评分组的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartRootName, Type = typeof(FrameworkElement))]
public sealed class CheemsStarRating : Control
{
    private const string PartRootName = "PartRoot";
    private static readonly KeySpline CssEase = new(0.25, 0.1, 0.25, 1);

    private readonly RadioButton?[] _stars = new RadioButton?[5];
    private readonly TextBlock?[] _glyphs = new TextBlock?[5];
    private FrameworkElement? _root;
    private bool _synchronizing;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(CheemsStarRating),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceValue));

    static CheemsStarRating()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsStarRating),
            new FrameworkPropertyMetadata(typeof(CheemsStarRating)));
    }

    /// <summary>当前评分，取值范围为 0 到 5；0 表示尚未选择。</summary>
    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public override void OnApplyTemplate()
    {
        DetachTemplateEvents();
        base.OnApplyTemplate();

        _root = GetTemplateChild(PartRootName) as FrameworkElement;
        if (_root is not null)
        {
            _root.MouseLeave += Root_MouseLeave;
        }

        for (var index = 0; index < 5; index++)
        {
            var number = index + 1;
            _stars[index] = GetTemplateChild($"PartStar{number}") as RadioButton;

            if (_stars[index] is not null)
            {
                _stars[index]!.Tag = number;
                _stars[index]!.Checked += Star_Checked;
                _stars[index]!.MouseEnter += Star_MouseEnter;
                _stars[index]!.ApplyTemplate();
                _glyphs[index] = _stars[index]!.Template.FindName("PartItemGlyph", _stars[index]) as TextBlock;
            }

            if (_glyphs[index]?.Foreground is SolidColorBrush foreground)
            {
                _glyphs[index]!.Foreground = foreground.CloneCurrentValue();
            }
        }

        SynchronizeSelection(animate: false);
    }

    private static object CoerceValue(DependencyObject dependencyObject, object baseValue)
    {
        return Math.Max(0, Math.Min(5, (int)baseValue));
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsStarRating)dependencyObject).SynchronizeSelection(animate: true);
    }

    private void Star_Checked(object sender, RoutedEventArgs e)
    {
        if (_synchronizing || sender is not RadioButton { Tag: int value })
        {
            return;
        }

        Value = value;
    }

    private void Star_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is RadioButton { Tag: int value })
        {
            // CSS 中 checked 与 hover 选择器同时生效，因此高亮范围是两者的并集。
            AnimateHighlightCount(Math.Max(Value, value));
        }
    }

    private void Root_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimateHighlightCount(Value);
    }

    private void SynchronizeSelection(bool animate)
    {
        _synchronizing = true;
        try
        {
            for (var index = 0; index < _stars.Length; index++)
            {
                if (_stars[index] is not null)
                {
                    _stars[index]!.IsChecked = index + 1 == Value;
                }
            }
        }
        finally
        {
            _synchronizing = false;
        }

        if (animate)
        {
            AnimateHighlightCount(Value);
        }
        else
        {
            SetHighlightCountImmediate(Value);
        }
    }

    private void AnimateHighlightCount(int count)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            SetHighlightCountImmediate(count);
            return;
        }

        var idle = FindColor(CheemsKeys.StarRatingIdleColor);
        var active = FindColor(CheemsKeys.StarRatingActiveColor);

        for (var index = 0; index < _glyphs.Length; index++)
        {
            if (_glyphs[index]?.Foreground is not SolidColorBrush brush)
            {
                continue;
            }

            var target = index < count ? active : idle;
            var animation = new ColorAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new DiscreteColorKeyFrame(brush.Color, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new SplineColorKeyFrame(
                target,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)),
                CssEase));
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }

    private void SetHighlightCountImmediate(int count)
    {
        var idle = FindColor(CheemsKeys.StarRatingIdleColor);
        var active = FindColor(CheemsKeys.StarRatingActiveColor);

        for (var index = 0; index < _glyphs.Length; index++)
        {
            if (_glyphs[index]?.Foreground is not SolidColorBrush brush)
            {
                continue;
            }

            brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            brush.Color = index < count ? active : idle;
        }
    }

    private Color FindColor(string key)
    {
        return (Color)FindResource(key);
    }

    private void DetachTemplateEvents()
    {
        if (_root is not null)
        {
            _root.MouseLeave -= Root_MouseLeave;
            _root = null;
        }

        for (var index = 0; index < _stars.Length; index++)
        {
            if (_stars[index] is not null)
            {
                _stars[index]!.Checked -= Star_Checked;
                _stars[index]!.MouseEnter -= Star_MouseEnter;
            }

            _stars[index] = null;
            _glyphs[index] = null;
        }
    }
}
