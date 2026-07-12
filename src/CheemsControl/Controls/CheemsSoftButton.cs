using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>
/// Uiverse ke1221 软拟态按钮的 WPF 等价实现。
/// </summary>
public sealed class CheemsSoftButton : Button
{
    static CheemsSoftButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsSoftButton),
            new FrameworkPropertyMetadata(typeof(CheemsSoftButton)));
    }
}
