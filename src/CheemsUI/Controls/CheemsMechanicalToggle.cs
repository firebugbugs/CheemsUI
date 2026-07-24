using System.Windows;
using System.Windows.Controls.Primitives;

namespace CheemsUI;

/// <summary>
/// Uiverse tunminh_6850 机械拟态开关的 WPF 等价实现。
/// </summary>
/// <remarks>
/// 基准本体为 80×40，拨块为 32×32，状态切换采用原版 0.4 秒
/// cubic-bezier(0.175, 0.885, 0.32, 1.275) 过冲曲线。
/// </remarks>
public sealed class CheemsMechanicalToggle : ToggleButton
{
    static CheemsMechanicalToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsMechanicalToggle),
            new FrameworkPropertyMetadata(typeof(CheemsMechanicalToggle)));
    }
}
