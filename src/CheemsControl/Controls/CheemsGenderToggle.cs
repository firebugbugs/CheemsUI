using System.Windows;
using System.Windows.Controls.Primitives;

namespace CheemsControl;

/// <summary>
/// Uiverse anand_4957 性别符号切换开关的 WPF 等价实现。
/// </summary>
/// <remarks>
/// 保留 233×88 内容区、8px 轨道边框、两个重叠伪元素滑块和轨道背后的符号层。
/// </remarks>
public sealed class CheemsGenderToggle : ToggleButton
{
    static CheemsGenderToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsGenderToggle),
            new FrameworkPropertyMetadata(typeof(CheemsGenderToggle)));
    }
}
