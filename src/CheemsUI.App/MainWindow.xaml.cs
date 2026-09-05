using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CheemsUI.App.Infrastructure;
using CheemsUI.App.Infrastructure.Updates;
using CheemsUI.App.ViewModels;

namespace CheemsUI.App;

/// <summary>
/// 导航壳（规矩 M1）：code-behind 只挂 DataContext，不含业务。
/// </summary>
public partial class MainWindow : Window
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmBorderColor = 34;
    private const uint DwmCornerRound = 2;
    private const uint DwmColorNone = 0xFFFFFFFE;
    private static readonly Color DefaultBackgroundColor = Color.FromRgb(0xE8, 0xE8, 0xE8);
    private readonly UpdateService _updateService = new();
    private BackgroundProfileViewModel? _activeBackgroundProfile;
    private HwndSource? _windowSource;
    private IntPtr _windowHandle;
    internal WindowThemeViewModel WindowTheme { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ApplyPaletteForBackground(DefaultBackgroundColor);
        var viewModel = new MainViewModel();
        viewModel.BackgroundSettings.SettingsChanged += (_, _) => ApplyCurrentBackgroundPalette();
        DataContext = viewModel;
        UpdateWindowFrameClip();
    }

    private void TitleAvatar_MouseEnter(object sender, MouseEventArgs e)
    {
        var currentAngle = TitleAvatarRotate.Angle;
        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(TitleAvatarScale.ScaleX, 1.16, 180, new CubicEase { EasingMode = EasingMode.EaseOut }, holdEnd: true));
        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(TitleAvatarScale.ScaleY, 1.16, 180, new CubicEase { EasingMode = EasingMode.EaseOut }, holdEnd: true));
        TitleAvatarRotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation(currentAngle, currentAngle + 360, TimeSpan.FromMilliseconds(1600))
            {
                RepeatBehavior = RepeatBehavior.Forever
            });
    }

    private void TitleAvatar_MouseLeave(object sender, MouseEventArgs e)
    {
        var currentScaleX = TitleAvatarScale.ScaleX;
        var currentScaleY = TitleAvatarScale.ScaleY;
        var currentAngle = TitleAvatarRotate.Angle;
        var returnAngle = Math.Round(currentAngle / 360d) * 360d;
        var returnEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.22 };

        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        TitleAvatarRotate.BeginAnimation(RotateTransform.AngleProperty, null);

        TitleAvatarScale.ScaleX = 1;
        TitleAvatarScale.ScaleY = 1;
        TitleAvatarRotate.Angle = 0;

        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(currentScaleX, 1, 340, returnEase));
        TitleAvatarScale.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(currentScaleY, 1, 340, returnEase));
        TitleAvatarRotate.BeginAnimation(RotateTransform.AngleProperty, CreateAnimation(currentAngle, returnAngle, 420, returnEase));
    }

    private static DoubleAnimation CreateAnimation(double from, double to, double milliseconds, IEasingFunction easingFunction, bool holdEnd = false) =>
        new(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = easingFunction,
            FillBehavior = holdEnd ? FillBehavior.HoldEnd : FillBehavior.Stop
        };

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowSource = (HwndSource)PresentationSource.FromVisual(this);
        _windowHandle = _windowSource.Handle;
        _windowSource.AddHook(WindowMessageHook);
        ApplyNativeWindowAppearance();
        UpdateWindowFrameClip();
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo)
        {
            return IntPtr.Zero;
        }

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return IntPtr.Zero;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.WorkArea;
        var monitorArea = monitorInfo.MonitorArea;
        minMaxInfo.MaxPosition.X = workArea.Left - monitorArea.Left;
        minMaxInfo.MaxPosition.Y = workArea.Top - monitorArea.Top;
        minMaxInfo.MaxSize.X = workArea.Right - workArea.Left;
        minMaxInfo.MaxSize.Y = workArea.Bottom - workArea.Top;
        minMaxInfo.MaxTrackSize = minMaxInfo.MaxSize;
        Marshal.StructureToPtr(minMaxInfo, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    public void ApplyBirdsBackground()
    {
        SetActiveBackground(PartBirdsWindowBackgroundOverlay, GetBackgroundSettings().Birds);
    }

    public void ApplyCloudsBackground()
    {
        SetActiveBackground(PartCloudsWindowBackgroundOverlay, GetBackgroundSettings().Clouds);
    }

    public void ApplyCellsBackground()
    {
        SetActiveBackground(PartCellsWindowBackgroundOverlay, GetBackgroundSettings().Cells);
    }

    public void ApplyDotsBackground()
    {
        SetActiveBackground(PartDotsWindowBackgroundOverlay, GetBackgroundSettings().Dots);
    }

    public void ApplyRisoDitherBackground()
    {
        SetActiveBackground(PartRisoDitherWindowBackgroundOverlay, GetBackgroundSettings().RisoDither);
    }

    public void RestoreDefaultBackground()
    {
        PartBirdsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCloudsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartDotsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartCellsWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        PartRisoDitherWindowBackgroundOverlay.Visibility = Visibility.Collapsed;
        _activeBackgroundProfile = null;
        ApplyPaletteForBackground(DefaultBackgroundColor);
        WindowFrame.SetResourceReference(BackgroundProperty, "App.Window.Background");
        SetResourceReference(BackgroundProperty, "App.Window.Background");
    }

    /// <summary>
    /// Applies a hue-aware, contrast-safe palette from the colour users actually see after
    /// the background overlay is composited.
    /// </summary>
    public void ApplyPaletteForBackground(Color backgroundColor)
    {
        var palette = AdaptiveThemePalette.Create(
            backgroundColor,
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

    private BackgroundsViewModel GetBackgroundSettings() =>
        ((MainViewModel)DataContext).BackgroundSettings;

    private void SetActiveBackground(FrameworkElement activeOverlay, BackgroundProfileViewModel profile)
    {
        FrameworkElement[] overlays =
        [
            PartBirdsWindowBackgroundOverlay,
            PartCloudsWindowBackgroundOverlay,
            PartDotsWindowBackgroundOverlay,
            PartCellsWindowBackgroundOverlay,
            PartRisoDitherWindowBackgroundOverlay
        ];
        foreach (var overlay in overlays)
        {
            overlay.Visibility = ReferenceEquals(overlay, activeOverlay) ? Visibility.Visible : Visibility.Collapsed;
        }

        _activeBackgroundProfile = profile;
        ApplyCurrentBackgroundPalette();
    }

    private void ApplyCurrentBackgroundPalette()
    {
        if (_activeBackgroundProfile is not { } settings)
        {
            return;
        }

        var effectBackground = settings.SupportsBirdsSettings
            ? settings.BirdsBackgroundColor
            : settings.SupportsCloudsSettings
                ? settings.CloudsSkyColor
                : settings.SupportsDotsSettings
                    ? settings.DotsBackgroundColor
                    : settings.PrimaryColor;
        var effectOpacity = settings.BackgroundOpacity *
            (settings.SupportsBirdsSettings ? settings.BirdsBackgroundAlpha : 1d);
        ApplyPaletteForBackground(BlendWithDefaultBackground(effectBackground, effectOpacity));
    }

    private static Color BlendWithDefaultBackground(Color overlay, double opacity)
    {
        byte BlendChannel(byte background, byte foreground) => (byte)Math.Round(
            background + (foreground - background) * opacity,
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

        if (PartDotsWindowBackgroundOverlay.Visibility == Visibility.Visible)
        {
            PartDotsWindowBackground.SetPointerPosition(e.GetPosition(PartDotsWindowBackground));
            return;
        }

        if (PartCellsWindowBackgroundOverlay.Visibility == Visibility.Visible)
        {
            PartCellsWindowBackground.SetPointerPosition(e.GetPosition(PartCellsWindowBackground));
        }
    }

    private void WindowFrame_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowFrameClip();

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        UpdateWindowFrameClip();
        PartCloudsWindowBackground.IsFullScreen = WindowState == WindowState.Maximized;
        PartDotsWindowBackground.IsFullScreen = WindowState == WindowState.Maximized;
    }

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
        ApplyNativeWindowRegion(radius);
    }

    private void ApplyNativeWindowAppearance()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        // Windows 11：保留系统阴影和圆角策略，但关闭系统额外描边，界面只显示自己的边框。
        var cornerPreference = DwmCornerRound;
        _ = DwmSetWindowAttribute(
            _windowHandle, DwmWindowCornerPreference, ref cornerPreference, Marshal.SizeOf<uint>());
        var borderColor = DwmColorNone;
        _ = DwmSetWindowAttribute(
            _windowHandle, DwmBorderColor, ref borderColor, Marshal.SizeOf<uint>());
    }

    private void ApplyNativeWindowRegion(double radius)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            // 最大化由 WM_GETMINMAXINFO 限制在工作区，使用矩形 HWND，不能覆盖任务栏。
            _ = SetWindowRgn(_windowHandle, IntPtr.Zero, true);
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var width = Math.Max(1, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY));
        var diameterX = Math.Max(1, (int)Math.Round(radius * 2 * dpi.DpiScaleX));
        var diameterY = Math.Max(1, (int)Math.Round(radius * 2 * dpi.DpiScaleY));
        var region = CreateRoundRectRgn(0, 0, width + 1, height + 1, diameterX, diameterY);
        if (region == IntPtr.Zero)
        {
            return;
        }

        // SetWindowRgn 成功后区域所有权转交给 Windows；失败时才由当前进程释放。
        if (SetWindowRgn(_windowHandle, region, true) == 0)
        {
            _ = DeleteObject(region);
        }
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
        _windowSource?.RemoveHook(WindowMessageHook);
        _windowSource = null;
        _windowHandle = IntPtr.Zero;

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CancelGifExport();
        }

        base.OnClosed(e);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr objectHandle);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref uint attributeValue, int attributeSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }
}
