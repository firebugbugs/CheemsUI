using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
    private readonly Dictionary<Button, int> _copyTokens = [];
    private int _hoverToken;

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

    private void DotsBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyDotsBackground();
    }

    private void RisoDitherBackground_ApplyRequested(object? sender, EventArgs e)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ApplyRisoDitherBackground();
    }

    private async void BackgroundPreview_Frozen(object? sender, EventArgs e)
    {
        if (sender is not WebView2CompositionControl background || background.CoreWebView2 is null)
        {
            return;
        }

        var target = sender switch
        {
            CheemsBirdsBackground => BirdsPreviewImage,
            CheemsCloudsBackground => CloudsPreviewImage,
            CheemsDotsBackground => DotsPreviewImage,
            CheemsCellsBackground => CellsPreviewImage,
            CheemsRisoDitherBackground => RisoDitherPreviewImage,
            _ => null
        };
        if (target is null || target.Source is not null)
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
