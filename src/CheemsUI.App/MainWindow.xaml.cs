using System.Windows;
using System.Windows.Controls;
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
    private static readonly Color CloudsBackgroundColor = Color.FromRgb(0x68, 0xB8, 0xD7);
    private static readonly Color CellsBackgroundColor = Color.FromRgb(0xD7, 0xFF, 0x8F);
    private static readonly Color RisoDitherBackgroundColor = Color.FromRgb(0x0A, 0x0E, 0x23);
    private const double BackgroundOverlayOpacity = 0.8;
    private readonly UpdateService _updateService = new();
    internal WindowThemeViewModel WindowTheme { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ApplyPaletteForBackground(DefaultBackgroundColor);
        DataContext = new MainViewModel();
        UpdateWindowFrameClip();
    }

    public void ApplyBirdsBackground()
    {
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Visible;
        ApplyPaletteForBackground(BlendWithDefaultBackground(BirdsBackgroundColor), BirdsBackgroundColor);
    }

    public void ApplyCloudsBackground()
    {
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Visible;
        ApplyPaletteForBackground(BlendWithDefaultBackground(CloudsBackgroundColor), CloudsBackgroundColor);
    }

    public void ApplyCellsBackground()
    {
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Visible;
        ApplyPaletteForBackground(BlendWithDefaultBackground(CellsBackgroundColor), CellsBackgroundColor);
    }

    public void ApplyRisoDitherBackground()
    {
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Visible;
        ApplyPaletteForBackground(BlendWithDefaultBackground(RisoDitherBackgroundColor), RisoDitherBackgroundColor);
    }

    public void RestoreDefaultBackground()
    {
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        ApplyPaletteForBackground(DefaultBackgroundColor);
        WindowFrame.SetResourceReference(BackgroundProperty, "App.Window.Background");
        SetResourceReference(BackgroundProperty, "App.Window.Background");
    }

    /// <summary>
    /// Applies a hue-aware, contrast-safe palette. Brightness is based on the colour users
    /// actually see after the background overlay is composited; hue comes from the effect itself.
    /// </summary>
    public void ApplyPaletteForBackground(Color backgroundColor, Color? tintSource = null)
    {
        var palette = AdaptiveThemePalette.Create(
            tintSource ?? backgroundColor,
            isDark: GetRelativeLuminance(backgroundColor) < 0.42);

        WindowTheme.Apply(palette);
        // 动态背景只作为默认浅灰底色上的半透明叠层，不能替换底层颜色。
        SetBrushColor("App.Window.Background", DefaultBackgroundColor);
        SetBrushColor("App.Window.Border", palette.WindowBorder);
        SetBrushColor("App.Sidebar.Background", palette.SidebarBackground);
        SetBrushColor("App.Navigation.Hover", palette.NavigationHover);
        SetBrushColor("App.Navigation.Selected", palette.NavigationSelected);
        SetBrushColor("App.Text.Primary", palette.PrimaryText);
        SetBrushColor("App.Text.Secondary", palette.SecondaryText);
        SetBrushColor("App.Text.OnBackground.Primary", palette.OnBackgroundPrimaryText);
        SetBrushColor("App.Text.OnBackground.Secondary", palette.OnBackgroundSecondaryText);
        SetBrushColor("App.Accent", palette.Accent);
        SetBrushColor("App.TitleBar.Icon.Background", palette.TitleBarIconBackground);
        SetBrushColor("App.TitleBar.Button.Foreground", palette.TitleBarButtonForeground);
        SetBrushColor("App.TitleBar.Button.Hover", palette.TitleBarButtonHover);
        SetBrushColor("App.TitleBar.Button.Pressed", palette.TitleBarButtonPressed);
        SetBrushColor("App.Search.Background", palette.SearchBackground);
        SetBrushColor("App.Search.Border", palette.SearchBorder);
        SetBrushColor("App.Search.Icon", palette.SearchIcon);
        SetBrushColor("App.Sidebar.Footer.Background", palette.SidebarFooterBackground);
        SetBrushColor("App.Notification.Background", palette.NotificationBackground);
        SetBrushColor("App.Notification.Border", palette.NotificationBorder);
        SetBrushColor("Cheems.Brush.Accent", palette.Accent);
        SetBrushColor("Cheems.Brush.Text.Primary", palette.PrimaryText);
        SetBrushColor("Cheems.Brush.Text.Secondary", palette.SecondaryText);
        SetBrushColor("Cheems.Brush.Background.Default", palette.Surface);
        SetBrushColor("Cheems.Brush.Background.Elevated", palette.ElevatedSurface);
        SetBrushColor("Cheems.Brush.Border.Default", palette.Border);
    }

    private void SetBrushColor(string resourceKey, Color color)
    {
        Resources[resourceKey] = new SolidColorBrush(color);
    }

    private static Color BlendWithDefaultBackground(Color overlay)
    {
        static byte BlendChannel(byte background, byte foreground) => (byte)Math.Round(
            background + (foreground - background) * BackgroundOverlayOpacity,
            MidpointRounding.AwayFromZero);

        return Color.FromRgb(
            BlendChannel(DefaultBackgroundColor.R, overlay.R),
            BlendChannel(DefaultBackgroundColor.G, overlay.G),
            BlendChannel(DefaultBackgroundColor.B, overlay.B));
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
        if (PartBirdsWindowBackgroundOverlay.Visibility == Visibility.Visible)
        {
            PartBirdsWindowBackground.SetPointerPosition(e.GetPosition(PartBirdsWindowBackground));
            return;
        }

        if (PartCloudsWindowBackgroundOverlay.Visibility == Visibility.Visible)
        {
            PartCloudsWindowBackground.SetPointerPosition(e.GetPosition(PartCloudsWindowBackground));
            return;
        }

        if (PartCellsWindowBackgroundOverlay.Visibility == Visibility.Visible)
        {
            PartCellsWindowBackground.SetPointerPosition(e.GetPosition(PartCellsWindowBackground));
        }
    }

    private void WindowFrame_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowFrameClip();

    private void Window_StateChanged(object? sender, EventArgs e) => UpdateWindowFrameClip();

    private void UpdateWindowFrameClip()
    {
        if (WindowFrame.ActualWidth <= 0 || WindowFrame.ActualHeight <= 0)
        {
            return;
        }

        var radius = WindowState == WindowState.Maximized ? 0d : 12d;
        var frameClip = new RectangleGeometry(
            new Rect(0, 0, WindowFrame.ActualWidth, WindowFrame.ActualHeight), radius, radius);

        // Window 的 Background 位于 WindowFrame 之后；两层必须共享同一裁剪边界，
        // 否则圆角外侧会露出根背景色。
        WindowFrame.Clip = frameClip;
        Clip = frameClip.Clone();
    }

    private void UpdateMenuButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = GetUpdateMenu();
        menu.DataContext = WindowTheme;
        menu.PlacementTarget = UpdateMenuButton;
        menu.IsOpen = true;
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        GetUpdateMenu().IsOpen = false;
        UpdateMenuButton.IsEnabled = false;
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            switch (result.State)
            {
                case UpdateCheckState.NoUpdate:
                    AppDialog.Show(this, WindowTheme, new AppDialogOptions(
                        "已是最新版本",
                        $"当前版本 {result.CurrentVersion} 已是可用的最新稳定版。"));
                    break;

                case UpdateCheckState.ConnectionFailed:
                    AppDialog.Show(this, WindowTheme, new AppDialogOptions(
                        "无法检查更新",
                        result.Message ?? "暂时无法连接更新服务，请稍后重试。",
                        AppDialogKind.Warning));
                    break;

                case UpdateCheckState.ReleaseUnavailable:
                    AppDialog.Show(this, WindowTheme, new AppDialogOptions(
                        "发行版暂不可用",
                        result.Message ?? "最新发行版缺少可用安装包，请稍后重试。",
                        AppDialogKind.Warning));
                    break;

                case UpdateCheckState.UpdateAvailable when result.Release is not null:
                    var notes = string.IsNullOrWhiteSpace(result.Release.Notes)
                        ? "本次发行未提供更新说明。"
                        : result.Release.Notes.Trim();
                    if (AppDialog.Show(this, WindowTheme, new AppDialogOptions(
                            $"发现新版本 {result.Release.Version}",
                            $"当前版本：{result.CurrentVersion}\n\n{notes}\n\n下载完成后将校验文件完整性，并关闭当前程序启动安装。",
                            AppDialogKind.Question,
                            "下载并安装",
                            "稍后")))
                    {
                        new UpdateDownloadWindow(this, WindowTheme, _updateService, result.Release).Show();
                    }
                    break;
            }
        }
        finally
        {
            UpdateMenuButton.IsEnabled = true;
        }
    }

    private ContextMenu GetUpdateMenu() => (ContextMenu)FindResource("App.UpdateMenu");

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        GetUpdateMenu().IsOpen = false;
        new AboutWindow(this, WindowTheme).ShowDialog();
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
