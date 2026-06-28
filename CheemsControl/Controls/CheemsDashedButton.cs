using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>
/// 虚线边框按钮（移植自 Uiverse by Praashoo7 的 CSS 按钮）。
/// <para>米色底 + 虚线描边 + “底色光环与投影分离”的双层框效果；按下时整体位移、投影收紧（0.1s 动画）。
/// 圆角为固定设计值（15px），不提供 CornerRadius 定制；配色经 L1/L2 语义键主题化。</para>
/// </summary>
public class CheemsDashedButton : Button
{
    static CheemsDashedButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsDashedButton),
            new FrameworkPropertyMetadata(typeof(CheemsDashedButton)));
    }
}
