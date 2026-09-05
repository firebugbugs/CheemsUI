using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CheemsUI.App.Backgrounds;
using CheemsUI.App.Infrastructure;
using CheemsUI.App.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CheemsUI.App.Pages;

public partial class BackgroundsPage : UserControl
{
    private const string BirdsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Birds.xaml.txt";
    private const string CloudsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Clouds.xaml.txt";
    private const string DotsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Dots.xaml.txt";
    private const string CellsSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Cells.xaml.txt";
    private const string RisoDitherSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/RisoDither.xaml.txt";
    private const string CubesSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Cubes.xaml.txt";
    private const string MatrixSourceUri = "/CheemsUI.App;component/Sources/Backgrounds/Matrix.xaml.txt";
    private readonly Dictionary<Button, int> _copyTokens = [];
    private int _hoverToken;
    private bool _previewStaggerStarted;

    public BackgroundsPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    /// <summary>
    /// WebView 背景卡错峰初始化：5 个 WebView2 同时启动会在 UI/渲染线程造成明显卡顿，
    /// 这里在占位层下逐张唤醒（宿主由 Collapsed 变 Visible 才触发控件初始化与冻结截图），
    /// 等上一张完成静态快照后再放行下一张；4 秒超时兜底防止个别卡失败时堵住整队。
    /// </summary>
    private async void OnPageLoaded(object? sender, RoutedEventArgs e)
    {
        if (_previewStaggerStarted)
        {
            return;
        }

        _previewStaggerStarted = true;
        var steps = new (FrameworkElement Host, Func<bool> Frozen)[]
        {
            (PartBirdsPreviewHost, () => BirdsPreviewImage.Source is not null),
            (PartDotsPreviewHost, () => DotsPreviewImage.Source is not null),
            (PartCloudsPreviewHost, () => CloudsPreviewImage.Source is not null),
            (PartCellsPreviewHost, () => CellsPreviewImage.Source is not null),
            (PartRisoDitherPreviewHost, () => RisoDitherPreviewImage.Source is not null)
        };
        foreach (var step in steps)
        {
            // 页面被切走时暂停推进，避免剩余卡片在重新挂载时一齐初始化。
            while (!IsLoaded)
            {
                await Task.Delay(200);
            }

            step.Host.Visibility = System.Windows.Visibility.Visible;
            var deadline = Environment.TickCount64 + 4000;
            while (!step.Frozen() && Environment.TickCount64 < deadline)
            {
                await Task.Delay(120);
            }

            await Task.Delay(100);
        }
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

    private void DotsBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyDotsBackground();
    }

    private void RisoDitherBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyRisoDitherBackground();
    }

    private void CubesBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyCubesBackground();
    }

    /// <summary>
    /// 数字雨卡片在占位层下静默运行零点几秒后，把当前帧渲染成位图并立即换成静态 Image，
    /// 与 WebView 背景卡的静态预览冻结管线完全一致（用户看不到活动动画，卡片不再消耗渲染资源）。
    /// </summary>
    private void MatrixPreview_Frozen(object? sender, EventArgs e)
    {
        if (sender is not CheemsUI.CheemsMatrixRainBackground background ||
            MatrixPreviewImage.Source is not null)
        {
            return;
        }

        try
        {
            var dpi = VisualTreeHelper.GetDpi(background);
            var width = Math.Max(1, (int)Math.Ceiling(background.ActualWidth * dpi.PixelsPerDip));
            var height = Math.Max(1, (int)Math.Ceiling(background.ActualHeight * dpi.PixelsPerDip));
            var bitmap = new RenderTargetBitmap(
                width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bitmap.Render(background);
            bitmap.Freeze();

            MatrixPreviewImage.Source = bitmap;
            MatrixPreviewImage.Visibility = System.Windows.Visibility.Visible;
            background.Visibility = System.Windows.Visibility.Collapsed;
            MatrixPreviewPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void MatrixBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyMatrixBackground();
    }

    private async void BackgroundPreview_Frozen(object? sender, EventArgs e)
    {
        if (sender is not WebView2CompositionControl background || background.CoreWebView2 is null)
        {
            return;
        }

        var (target, placeholder) = sender switch
        {
            CheemsBirdsBackground => (BirdsPreviewImage, BirdsPreviewPlaceholder),
            CheemsCloudsBackground => (CloudsPreviewImage, CloudsPreviewPlaceholder),
            CheemsDotsBackground => (DotsPreviewImage, DotsPreviewPlaceholder),
            CheemsCellsBackground => (CellsPreviewImage, CellsPreviewPlaceholder),
            CheemsRisoDitherBackground => (RisoDitherPreviewImage, RisoDitherPreviewPlaceholder),
            _ => ((Image?)null, (FrameworkElement?)null)
        };
        if (target is null || placeholder is null || target.Source is not null)
        {
            return;
        }

        try
        {
            // 等待 data URL 图片完成一次浏览器合成，避免刚替换 canvas 时截到空白帧。
            await Task.Delay(50);
            using var stream = new MemoryStream();
            await background.CoreWebView2.CapturePreviewAsync(
                CoreWebView2CapturePreviewImageFormat.Png, stream);
            stream.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            target.Source = bitmap;
            target.Visibility = System.Windows.Visibility.Visible;
            background.Visibility = System.Windows.Visibility.Collapsed;
            placeholder.Visibility = System.Windows.Visibility.Collapsed;
            ((IStaticPreviewWebView)background).ReleasePreview();
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void StaticPreview_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string key } ||
            System.Windows.Window.GetWindow(this) is not MainWindow window)
        {
            return;
        }

        switch (key)
        {
            case "Birds": window.ApplyBirdsBackground(); break;
            case "Clouds": window.ApplyCloudsBackground(); break;
            case "Dots": window.ApplyDotsBackground(); break;
            case "Cells": window.ApplyCellsBackground(); break;
            case "RisoDither": window.ApplyRisoDitherBackground(); break;
            case "Cubes": window.ApplyCubesBackground(); break;
            case "Matrix": window.ApplyMatrixBackground(); break;
        }
        e.Handled = true;
    }

    private void RestoreBackground_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.RestoreDefaultBackground();
    }

    private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string key } button ||
            DataContext is not BackgroundsViewModel viewModel ||
            viewModel.GetProfile(key) is not { } profile)
        {
            return;
        }

        // 复用一个 Popup，切换卡片时不会遗留多份参数窗或丢失绑定状态。
        SettingsPopup.IsOpen = false;
        SettingsPopup.DataContext = profile;
        SettingsPopup.PlacementTarget = button;
        SettingsPopup.IsOpen = true;
    }

    private void CodeButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _hoverToken++;
        CodeText.Text = SourceCodeService.Load(GetSourceUri(button));
        CodePopup.PlacementTarget = button;
        CodePopup.IsOpen = true;
    }

    private async void CodeButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var token = ++_hoverToken;
        await Task.Delay(80);

        // Popup 是独立窗口；打开瞬间可能令按钮短暂收到 MouseLeave。
        // 延迟后再次按屏幕指针位置确认，避免在打开/关闭之间循环闪烁。
        if (token == _hoverToken && !IsPointerInside(button))
        {
            CodePopup.IsOpen = false;
        }
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

    private async void DotsCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(DotsSourceUri, DotsCodeButton);
    }

    private async void RisoDitherCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(RisoDitherSourceUri, RisoDitherCodeButton);
    }

    private async void CubesCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(CubesSourceUri, CubesCodeButton);
    }

    private async void MatrixCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await CopySourceAsync(MatrixSourceUri, MatrixCodeButton);
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

    private string GetSourceUri(Button button) => button.Name switch
    {
        nameof(BirdsCodeButton) => BirdsSourceUri,
        nameof(CloudsCodeButton) => CloudsSourceUri,
        nameof(DotsCodeButton) => DotsSourceUri,
        nameof(CellsCodeButton) => CellsSourceUri,
        nameof(RisoDitherCodeButton) => RisoDitherSourceUri,
        nameof(CubesCodeButton) => CubesSourceUri,
        nameof(MatrixCodeButton) => MatrixSourceUri,
        _ => throw new ArgumentOutOfRangeException(nameof(button), "未知的背景源码按钮。")
    };

    private static bool IsPointerInside(Button button)
    {
        var position = Mouse.GetPosition(button);
        return position.X >= 0 && position.Y >= 0
            && position.X <= button.ActualWidth
            && position.Y <= button.ActualHeight;
    }
}
