using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 控件展示台：承载被演示控件，背景在浅（#E8E8E8）/ 深（#212121）之间切换，默认浅色。
/// 切换器为类库的 CheemsDayNightSwitch（选中 = 夜晚 = 深色）。颜色切换动画属纯视觉交互，留在 View 层（规矩 M1）。
/// </summary>
public partial class DemoArea : UserControl
{
    private static readonly Color LightStage = Color.FromRgb(0xE8, 0xE8, 0xE8);
    private static readonly Color DarkStage = Color.FromRgb(0x21, 0x21, 0x21);

    public static readonly DependencyProperty DemoProperty = DependencyProperty.Register(
        nameof(Demo), typeof(object), typeof(DemoArea), new PropertyMetadata(null));

    public DemoArea()
    {
        InitializeComponent();
    }

    /// <summary>被演示的控件内容。</summary>
    public object Demo
    {
        get => GetValue(DemoProperty)!;
        set => SetValue(DemoProperty, value);
    }

    private void Switch_Toggled(object sender, RoutedEventArgs e)
    {
        var dark = ((ToggleButton)sender).IsChecked == true;
        PartStageBrush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(dark ? DarkStage : LightStage, TimeSpan.FromSeconds(0.3)));
    }
}
