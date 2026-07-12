using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>Uiverse SmookyDev Sahara Tailwind Button 的 WPF 等价实现。</summary>
public sealed class CheemsSaharaButton : Button
{
    static CheemsSaharaButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsSaharaButton),
            new FrameworkPropertyMetadata(typeof(CheemsSaharaButton)));
    }
}
