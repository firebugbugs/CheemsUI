using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using CheemsUI.App.Infrastructure;
using CheemsUI.App.Infrastructure.Updates;
using CheemsUI.App.ViewModels;

namespace CheemsUI.App;

/// <summary>
/// 导航壳（规矩 M1）：code-behind 只挂 DataContext，不含业务。
/// </summary>
public partial class MainWindow : Window
{
    private static readonly Color DefaultBackgroundColor = Color.FromRgb(0xE8, 0xE8, 0xE8);
    private static readonly Color BirdsBackgroundColor = Color.FromRgb(0x07, 0x19, 0x2F);
    private readonly UpdateService _updateService = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    public void ApplyBirdsBackground()
    {
        PartBirdsWindowBackground.Visibility = Visibility.Visible;
        WindowFrame.Background = Brushes.Transparent;
        Background = Brushes.Transparent;
        ApplyPaletteForBackground(BirdsBackgroundColor);
    }

    public void RestoreDefaultBackground()
    {
        PartBirdsWindowBackground.Visibility = Visibility.Collapsed;
        ApplyPaletteForBackground(DefaultBackgroundColor);
        WindowFrame.SetResourceReference(BackgroundProperty, "App.Window.Background");
        SetResourceReference(BackgroundProperty, "App.Window.Background");
    }

    /// <summary>
    /// Applies the window chrome palette for a background's representative colour.
    /// New background effects can call this after they become active, without duplicating
    /// foreground, sidebar, search box, or caption button colour decisions.
    /// </summary>
    public void ApplyPaletteForBackground(Color backgroundColor)
    {
        if (GetRelativeLuminance(backgroundColor) < 0.42)
        {
            ApplyDarkPalette();
            return;
        }

        ApplyLightPalette();
    }

    private void ApplyLightPalette()
    {
        SetBrushColor("App.Window.Background", DefaultBackgroundColor);
        SetBrushColor("App.Window.Border", Color.FromRgb(0xD1, 0xD1, 0xD1));
        SetBrushColor("App.Sidebar.Background", Color.FromArgb(0xD9, 0xF5, 0xF5, 0xF5));
        SetBrushColor("App.Navigation.Hover", Color.FromArgb(0xBF, 0xEC, 0xEC, 0xEC));
        SetBrushColor("App.Navigation.Selected", Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
        SetBrushColor("App.Text.Primary", Color.FromRgb(0x20, 0x21, 0x24));
        SetBrushColor("App.Text.Secondary", Color.FromRgb(0x74, 0x77, 0x7C));
        SetBrushColor("App.Accent", Color.FromRgb(0x20, 0x21, 0x24));
        SetBrushColor("App.TitleBar.Icon.Background", Color.FromRgb(0x20, 0x21, 0x24));
        SetBrushColor("App.TitleBar.Button.Foreground", Color.FromRgb(0x4D, 0x4F, 0x53));
        SetBrushColor("App.TitleBar.Button.Hover", Color.FromRgb(0xDC, 0xDC, 0xDC));
        SetBrushColor("App.TitleBar.Button.Pressed", Color.FromRgb(0xD2, 0xD2, 0xD2));
        SetBrushColor("App.Search.Background", Colors.White);
        SetBrushColor("App.Search.Border", Color.FromRgb(0xE0, 0xE0, 0xE0));
        SetBrushColor("App.Search.Icon", Color.FromRgb(0x77, 0x7A, 0x7F));
        SetBrushColor("App.Sidebar.Footer.Background", Color.FromArgb(0xCC, 0xEC, 0xEC, 0xEC));
        SetBrushColor("App.Notification.Background", Colors.White);
        SetBrushColor("App.Notification.Border", Color.FromRgb(0xD6, 0xD9, 0xDE));
        SetBrushColor("Cheems.Brush.Text.Primary", Color.FromRgb(0x2D, 0x34, 0x36));
        SetBrushColor("Cheems.Brush.Text.Secondary", Color.FromRgb(0x63, 0x6E, 0x72));
        SetBrushColor("Cheems.Brush.Background.Default", Colors.White);
        SetBrushColor("Cheems.Brush.Background.Elevated", Color.FromRgb(0xF5, 0xF6, 0xFA));
        SetBrushColor("Cheems.Brush.Border.Default", Color.FromRgb(0xDF, 0xE6, 0xE9));
    }

    private void ApplyDarkPalette()
    {
        SetBrushColor("App.Window.Background", Color.FromRgb(0x07, 0x19, 0x2F));
        SetBrushColor("App.Window.Border", Color.FromRgb(0x5A, 0x70, 0x8B));
        SetBrushColor("App.Sidebar.Background", Color.FromArgb(0xD9, 0x0B, 0x20, 0x3A));
        SetBrushColor("App.Navigation.Hover", Color.FromArgb(0x75, 0x4A, 0x68, 0x8A));
        SetBrushColor("App.Navigation.Selected", Color.FromArgb(0xB8, 0x20, 0x3A, 0x5C));
        SetBrushColor("App.Text.Primary", Color.FromRgb(0xF4, 0xF7, 0xFB));
        SetBrushColor("App.Text.Secondary", Color.FromRgb(0xC0, 0xCE, 0xDD));
        SetBrushColor("App.Accent", Color.FromRgb(0x93, 0xD7, 0xFF));
        SetBrushColor("App.TitleBar.Icon.Background", Color.FromRgb(0x0D, 0x26, 0x43));
        SetBrushColor("App.TitleBar.Button.Foreground", Color.FromRgb(0xEB, 0xF4, 0xFF));
        SetBrushColor("App.TitleBar.Button.Hover", Color.FromArgb(0xC8, 0x4D, 0x69, 0x89));
        SetBrushColor("App.TitleBar.Button.Pressed", Color.FromArgb(0xE0, 0x36, 0x50, 0x6E));
        SetBrushColor("App.Search.Background", Color.FromArgb(0xB8, 0x0B, 0x20, 0x3A));
        SetBrushColor("App.Search.Border", Color.FromArgb(0xA0, 0xA8, 0xC0, 0xDA));
        SetBrushColor("App.Search.Icon", Color.FromRgb(0xD0, 0xE0, 0xF0));
        SetBrushColor("App.Sidebar.Footer.Background", Color.FromArgb(0xB8, 0x11, 0x2A, 0x47));
        SetBrushColor("App.Notification.Background", Color.FromRgb(0x16, 0x2D, 0x49));
        SetBrushColor("App.Notification.Border", Color.FromRgb(0x75, 0x91, 0xAF));
        SetBrushColor("Cheems.Brush.Text.Primary", Color.FromRgb(0xF4, 0xF7, 0xFB));
        SetBrushColor("Cheems.Brush.Text.Secondary", Color.FromRgb(0xC0, 0xCE, 0xDD));
        SetBrushColor("Cheems.Brush.Background.Default", Color.FromArgb(0xE8, 0x16, 0x2D, 0x49));
        SetBrushColor("Cheems.Brush.Background.Elevated", Color.FromArgb(0xE8, 0x20, 0x3B, 0x59));
        SetBrushColor("Cheems.Brush.Border.Default", Color.FromArgb(0xB8, 0x86, 0xA3, 0xC1));
    }

    private void SetBrushColor(string resourceKey, Color color)
    {
        Resources[resourceKey] = new SolidColorBrush(color);
    }

    private static double GetRelativeLuminance(Color color)
    {
        static double Linearize(byte channel)
        {
            var normalized = channel / 255d;
            return normalized <= 0.04045
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R)
             + 0.7152 * Linearize(color.G)
             + 0.0722 * Linearize(color.B);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (PartBirdsWindowBackground.Visibility == Visibility.Visible)
        {
            PartBirdsWindowBackground.SetPointerPosition(e.GetPosition(PartBirdsWindowBackground));
        }
    }

    private void UpdateMenuButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateMenu.IsOpen = true;
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        UpdateMenu.IsOpen = false;
        UpdateMenuButton.IsEnabled = false;
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            switch (result.State)
            {
                case UpdateCheckState.NoUpdate:
                    AppDialog.Show(this, new AppDialogOptions(
                        "已是最新版本",
                        $"当前版本 {result.CurrentVersion} 已是可用的最新稳定版。"));
                    break;

                case UpdateCheckState.ConnectionFailed:
                    AppDialog.Show(this, new AppDialogOptions(
                        "无法检查更新",
                        result.Message ?? "暂时无法连接更新服务，请稍后重试。",
                        AppDialogKind.Warning));
                    break;

                case UpdateCheckState.ReleaseUnavailable:
                    AppDialog.Show(this, new AppDialogOptions(
                        "发行版暂不可用",
                        result.Message ?? "最新发行版缺少可用安装包，请稍后重试。",
                        AppDialogKind.Warning));
                    break;

                case UpdateCheckState.UpdateAvailable when result.Release is not null:
                    var notes = string.IsNullOrWhiteSpace(result.Release.Notes)
                        ? "本次发行未提供更新说明。"
                        : result.Release.Notes.Trim();
                    if (AppDialog.Show(this, new AppDialogOptions(
                            $"发现新版本 {result.Release.Version}",
                            $"当前版本：{result.CurrentVersion}\n\n{notes}\n\n下载完成后将校验文件完整性，并关闭当前程序启动安装。",
                            AppDialogKind.Question,
                            "下载并安装",
                            "稍后")))
                    {
                        new UpdateDownloadWindow(this, _updateService, result.Release).Show();
                    }
                    break;
            }
        }
        finally
        {
            UpdateMenuButton.IsEnabled = true;
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
            return;
        }

        SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CancelGifExport();
        }

        base.OnClosed(e);
    }
}
