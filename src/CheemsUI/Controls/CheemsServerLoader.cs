using System.Windows;
using System.Windows.Controls;

namespace CheemsUI;

/// <summary>Uiverse Juanes200122 animated isometric server loader.</summary>
public sealed class CheemsServerLoader : Control
{
    static CheemsServerLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsServerLoader),
            new FrameworkPropertyMetadata(typeof(CheemsServerLoader)));
    }
}
