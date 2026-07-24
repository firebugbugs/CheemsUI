using System.Windows;
using System.Windows.Controls.Primitives;

namespace CheemsUI;

/// <summary>
/// Uiverse santhosh_2608 像素表情硬币开关的 WPF 等价实现。
/// </summary>
public sealed class CheemsPixelCoinSwitch : ToggleButton
{
    static CheemsPixelCoinSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsPixelCoinSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsPixelCoinSwitch)));
    }
}
