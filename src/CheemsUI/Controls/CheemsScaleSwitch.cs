using System.Windows;
using System.Windows.Controls.Primitives;

namespace CheemsUI;

/// <summary>
/// Uiverse Praashoo7 圆形缩放旋转开关的 WPF 等价实现。
/// </summary>
/// <remarks>
/// CSS 基准字号为 17px，因此控件尺寸为 34×34，内部图形为 23.8×23.8；
/// 选中时外圆缩放至 70%，内部图形从零缩放至完整尺寸并旋转 360°。
/// </remarks>
public sealed class CheemsScaleSwitch : ToggleButton
{
    static CheemsScaleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsScaleSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsScaleSwitch)));
    }
}
