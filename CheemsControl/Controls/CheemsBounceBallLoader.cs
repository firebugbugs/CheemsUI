using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>Uiverse mobinkakei Bounce Ball Loader 的 WPF 等价实现。</summary>
public sealed class CheemsBounceBallLoader : Control
{
    static CheemsBounceBallLoader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CheemsBounceBallLoader),
            new FrameworkPropertyMetadata(typeof(CheemsBounceBallLoader)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Bounce", false);
    }
}
