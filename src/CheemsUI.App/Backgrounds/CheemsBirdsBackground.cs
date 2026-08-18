using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CheemsUI.App.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CheemsUI.App.Backgrounds;

/// <summary>
/// 使用本地 three.js r134 与 Vanta BIRDS 原版脚本的离线背景层。
/// </summary>
public sealed class CheemsBirdsBackground : WebView2CompositionControl
{
    private const string VirtualHostName = "birds.cheemsui.local";
    private readonly Uri _birdsPage = new($"https://{VirtualHostName}/birds.offline.html");
    private Task? _initializationTask;
    private bool _isLoaded;
    private bool _isVantaReady;
    private long _lastPointerMessageTimestamp;

    /// <summary>
    /// 用户点击鸟群卡片时触发；页面可将当前效果应用到整个软件窗口。
    /// </summary>
    public event EventHandler? ApplyRequested;

    public CheemsBirdsBackground()
    {
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _isLoaded = true;

        try
        {
            _initializationTask ??= InitializeAsync();
            await _initializationTask;

            if (_isLoaded && CoreWebView2.Source != _birdsPage.AbsoluteUri)
            {
                CoreWebView2.Navigate(_birdsPage.AbsoluteUri);
            }
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _isLoaded = false;
        _isVantaReady = false;

        // Vanta 的 requestAnimationFrame 在空白页中不存在，避免页面离开后继续渲染。
        CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
    }

    private async Task InitializeAsync()
    {
        await EnsureCoreWebView2Async();

        var assetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "VantaBirds");
        if (!File.Exists(Path.Combine(assetFolder, "birds.offline.html")))
        {
            throw new FileNotFoundException("未找到离线 Vanta BIRDS 资源。", assetFolder);
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
        if ((now - _lastPointerMessageTimestamp) / (double)Stopwatch.Frequency < 1d / 60)
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

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            ErrorLog.Write(new InvalidOperationException($"离线 Vanta 页面导航失败：{e.WebErrorStatus}"));
            return;
        }

        try
        {
            var result = await CoreWebView2.ExecuteScriptAsync(
                "Boolean(window.VANTA && window.VANTA.BIRDS && document.querySelector('#birds-background canvas') && window.__cheemsSetPointer)");
            if (!string.Equals(result, "true", StringComparison.Ordinal))
            {
                ErrorLog.Write(new InvalidOperationException("离线 Vanta BIRDS 脚本未创建渲染画布。"));
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
