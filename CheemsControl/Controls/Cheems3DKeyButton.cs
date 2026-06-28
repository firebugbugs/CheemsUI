using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>Uiverse chintu_2484 三维键帽按钮的 WPF 等价实现。</summary>
public sealed class Cheems3DKeyButton : Button
{
    static Cheems3DKeyButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Cheems3DKeyButton),
            new FrameworkPropertyMetadata(typeof(Cheems3DKeyButton)));
    }
}
