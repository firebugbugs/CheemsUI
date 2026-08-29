using System.Windows.Controls;
using System.Windows.Interop;
using CheemsUI.App.Backgrounds;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.Pages;

public partial class BackgroundsPage : UserControl
{
    private const string BirdsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Birds.xaml.txt";
    private const string CloudsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Clouds.xaml.txt";
    private const string CellsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Cells.xaml.txt";
    private readonly Dictionary<Button, int> _copyTokens = [];

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

    private void CellsBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyCellsBackground();
    }

    private void RestoreBackground_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.RestoreDefaultBackground();
    }

    private async void BirdsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(BirdsSourceUri, BirdsCodeButton);
    }

    private async void CloudsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(CloudsSourceUri, CloudsCodeButton);
    }

    private async void CellsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(CellsSourceUri, CellsCodeButton);
    }

    private async Task CopySourceAsync(string sourceUri, Button button)
    {
        var token = _copyTokens.TryGetValue(button, out var currentToken) ? currentToken + 1 : 1;
        _copyTokens[button] = token;
        button.IsEnabled = false;
        button.Tag = "Copying";

        var window = System.Windows.Window.GetWindow(this);
        var ownerHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        var source = SourceCodeService.Load(sourceUri);
        var copied = await SourceCodeService.TryCopyToClipboardAsync(source, ownerHandle);

        button.Tag = copied ? "Success" : "Failure";
        button.ToolTip = copied ? "已复制 XAML" : "复制失败，请重试";
        button.IsEnabled = true;
        await Task.Delay(1200);
        if (_copyTokens[button] == token)
        {
            button.Tag = null;
        }
    }
}
