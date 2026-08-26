using System.Windows.Controls;
using System.Windows.Interop;
using CheemsUI.App.Backgrounds;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.Pages;

public partial class BackgroundsPage : UserControl
{
    private const string BirdsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Birds.xaml.txt";
    private const string CloudsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Clouds.xaml.txt";

    public BackgroundsPage()
    {
        InitializeComponent();
    }

    private void BirdsBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyBirdsBackground();
    }

    private void CloudsBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyCloudsBackground();
    }

    private void RestoreBackground_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.RestoreDefaultBackground();
    }

    private async void BirdsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        BirdsCodeButton.IsEnabled = false;

        var window = System.Windows.Window.GetWindow(this);
        var ownerHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        var source = SourceCodeService.Load(BirdsSourceUri);
        var copied = await SourceCodeService.TryCopyToClipboardAsync(source, ownerHandle);

        BirdsCodeButton.ToolTip = copied ? "已复制 XAML" : "复制失败，请重试";
        BirdsCodeButton.IsEnabled = true;
    }

    private async void CloudsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        CloudsCodeButton.IsEnabled = false;

        var window = System.Windows.Window.GetWindow(this);
        var ownerHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        var source = SourceCodeService.Load(CloudsSourceUri);
        var copied = await SourceCodeService.TryCopyToClipboardAsync(source, ownerHandle);

        CloudsCodeButton.ToolTip = copied ? "已复制 XAML" : "复制失败，请重试";
        CloudsCodeButton.IsEnabled = true;
    }
}
