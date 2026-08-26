using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CheemsUI.App.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CheemsUI.App.Backgrounds;

/// <summary>
/// 使用本地 three.js r134 与 Vanta CLOUDS 原版脚本的离线背景层。
/// </summary>
public sealed class CheemsCloudsBackground : WebView2CompositionControl
{
    private const string VirtualHostName = "clouds.cheemsui.local";
    private Task? _initializationTask;
    private bool _isLoaded;
    private bool _isVantaReady;
    private long _lastPointerMessageTimestamp;

    /// <summary>
    /// 用户点击云层卡片时触发；页面可将当前效果应用到整个软件窗口。
    /// </summary>
    public event EventHandler? ApplyRequested;

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsCloudsBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public CheemsCloudsBackground()
    {
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateClip();
    }

    /// <summary>当前 WebGL 表面的统一裁剪圆角。</summary>
    public double ClipRadius
    {
        get => (double)GetValue(ClipRadiusProperty);
        set => SetValue(ClipRadiusProperty, value);
    }

    private Uri CloudsPage => new($"https://{VirtualHostName}/clouds.offline.html");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        try
        {
            _initializationTask ??= InitializeAsync();
            await _initializationTask;

            NavigateToCloudsPage();
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        _isVantaReady = false;

        // Vanta 的 requestAnimationFrame 在空白页中不存在，避免页面离开后继续渲染。
        CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
    }

    private async Task InitializeAsync()
    {
        await EnsureCoreWebView2Async();

        var assetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "VantaClouds");
        if (!File.Exists(Path.Combine(assetFolder, "clouds.offline.html")))
        {
            throw new FileNotFoundException("未找到离线 Vanta CLOUDS 资源。", assetFolder);
        }

        CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName,
            assetFolder,
            CoreWebView2HostResourceAccessKind.DenyCors);
        CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        CoreWebView2.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "apply-background")
        {
            ApplyRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 将宿主窗口的鼠标坐标转发给本地 Vanta 页面。
    /// 组合宿主被 WPF 前景覆盖时，浏览器本身收不到该鼠标事件。
    /// </summary>
    public void SetPointerPosition(Point position)
    {
        if (CoreWebView2 is null || !_isVantaReady)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        if ((now - _lastPointerMessageTimestamp) / (double)Stopwatch.Frequency < 1d / 30)
        {
            return;
        }

        _lastPointerMessageTimestamp = now;
        var x = position.X.ToString("F2", CultureInfo.InvariantCulture);
        var y = position.Y.ToString("F2", CultureInfo.InvariantCulture);
        _ = SetPointerPositionAsync(x, y);
    }

    private async Task SetPointerPositionAsync(string x, string y)
    {
        try
        {
            await CoreWebView2.ExecuteScriptAsync($"window.__cheemsSetPointer?.({x}, {y});");
        }
        catch (InvalidOperationException)
        {
            // 页面切换时可能恰好失去 WebView；下一次 MouseMove 会重新发送。
        }
    }

    private static void OnClipRadiusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsCloudsBackground)dependencyObject).UpdateClip();
    }

    private void NavigateToCloudsPage()
    {
        _isVantaReady = false;
        if (_isLoaded && CoreWebView2 is not null && CoreWebView2.Source != CloudsPage.AbsoluteUri)
        {
            CoreWebView2.Navigate(CloudsPage.AbsoluteUri);
        }
    }

    private void UpdateClip()
    {
        if (ClipRadius <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            Clip = null;
            return;
        }

        Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius);
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (CoreWebView2.Source != CloudsPage.AbsoluteUri)
        {
            return;
        }

        if (!e.IsSuccess)
        {
            ErrorLog.Write(new InvalidOperationException($"离线 Vanta 页面导航失败：{e.WebErrorStatus}"));
            return;
        }

        try
        {
            var result = await CoreWebView2.ExecuteScriptAsync(
                "Boolean(window.VANTA && window.VANTA.CLOUDS && document.querySelector('#clouds-background canvas') && window.__cheemsSetPointer)");
            if (!string.Equals(result, "true", StringComparison.Ordinal))
            {
                ErrorLog.Write(new InvalidOperationException("离线 Vanta CLOUDS 脚本未创建渲染画布。"));
                return;
            }

            _isVantaReady = true;
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }
}
