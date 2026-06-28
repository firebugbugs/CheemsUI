using System.Windows;
using System.Windows.Controls.Primitives;

namespace CheemsControl;

/// <summary>
/// Uiverse chase2k25 LED 软拟态开关的 WPF 等价实现。
/// </summary>
public sealed class CheemsLedSwitch : ToggleButton
{
    static CheemsLedSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsLedSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsLedSwitch)));
    }
}
