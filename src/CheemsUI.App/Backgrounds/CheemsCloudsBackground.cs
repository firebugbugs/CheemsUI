using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
    private readonly DispatcherTimer _settingsUpdateTimer;

    /// <summary>
    /// 用户点击云层卡片时触发；页面可将当前效果应用到整个软件窗口。
    /// </summary>
    public event EventHandler? ApplyRequested;

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsCloudsBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsCloudsBackground),
        new PropertyMetadata(false, OnPageOptionChanged));

    public static readonly DependencyProperty IsFullScreenProperty = DependencyProperty.Register(
        nameof(IsFullScreen), typeof(bool), typeof(CheemsCloudsBackground),
        new PropertyMetadata(false, OnEffectOptionChanged));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register(
        nameof(PrimaryColor), typeof(Color), typeof(CheemsCloudsBackground),
        new PropertyMetadata(Colors.White, OnEffectOptionChanged));

    public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register(
        nameof(AnimationSpeed), typeof(double), typeof(CheemsCloudsBackground),
        new PropertyMetadata(1d, OnEffectOptionChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(CheemsCloudsBackground),
        new PropertyMetadata(true, OnEffectOptionChanged));

    public static readonly DependencyProperty SkyColorProperty = RegisterEffectOption<Color>(nameof(SkyColor), Color.FromRgb(0x68, 0xB8, 0xD7));
    public static readonly DependencyProperty CloudShadowColorProperty = RegisterEffectOption<Color>(nameof(CloudShadowColor), Color.FromRgb(0x18, 0x35, 0x50));
    public static readonly DependencyProperty SunColorProperty = RegisterEffectOption<Color>(nameof(SunColor), Color.FromRgb(0xFF, 0x99, 0x19));
    public static readonly DependencyProperty SunGlareColorProperty = RegisterEffectOption<Color>(nameof(SunGlareColor), Color.FromRgb(0xFF, 0x66, 0x33));
    public static readonly DependencyProperty SunlightColorProperty = RegisterEffectOption<Color>(nameof(SunlightColor), Color.FromRgb(0xFF, 0x99, 0x33));

    public CheemsCloudsBackground()
    {
        _settingsUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _settingsUpdateTimer.Tick += OnSettingsUpdateTimerTick;
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
        SizeChanged += (_, _) => UpdateClip();
    }

    /// <summary>当前 WebGL 表面的统一裁剪圆角。</summary>
    public double ClipRadius
    {
        get => (double)GetValue(ClipRadiusProperty);
        set => SetValue(ClipRadiusProperty, value);
    }

    /// <summary>使用更轻量的卡片预览渲染档位，避免多个 WebGL 预览争用 GPU。</summary>
    public bool IsPreview
    {
        get => (bool)GetValue(IsPreviewProperty);
        set => SetValue(IsPreviewProperty, value);
    }

    /// <summary>最大化时降低云层内部着色分辨率；最终画面仍由 Vanta 铺满窗口。</summary>
    public bool IsFullScreen
    {
        get => (bool)GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    public Color PrimaryColor { get => (Color)GetValue(PrimaryColorProperty); set => SetValue(PrimaryColorProperty, value); }

    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }

    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    public Color SkyColor { get => (Color)GetValue(SkyColorProperty); set => SetValue(SkyColorProperty, value); }
    public Color CloudShadowColor { get => (Color)GetValue(CloudShadowColorProperty); set => SetValue(CloudShadowColorProperty, value); }
    public Color SunColor { get => (Color)GetValue(SunColorProperty); set => SetValue(SunColorProperty, value); }
    public Color SunGlareColor { get => (Color)GetValue(SunGlareColorProperty); set => SetValue(SunGlareColorProperty, value); }
    public Color SunlightColor { get => (Color)GetValue(SunlightColorProperty); set => SetValue(SunlightColorProperty, value); }

    private static DependencyProperty RegisterEffectOption<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(CheemsCloudsBackground), new PropertyMetadata(defaultValue, OnEffectOptionChanged));

    private Uri CloudsPage => new($"https://{VirtualHostName}/clouds.offline.html{(IsPreview ? "?preview=1" : string.Empty)}");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        try
        {
            _initializationTask ??= InitializeAsync();
            await _initializationTask;

            if (IsVisible) NavigateToCloudsPage();
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
        _settingsUpdateTimer.Stop();

        // Vanta 的 requestAnimationFrame 在空白页中不存在，避免页面离开后继续渲染。
        CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            NavigateToCloudsPage();
        }
        else
        {
            _isVantaReady = false;
            CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
        }
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
        if ((now - _lastPointerMessageTimestamp) / (double)Stopwatch.Frequency < 1d / 60)
        {
            return;
        }

        _lastPointerMessageTimestamp = now;
        var x = position.X.ToString("F2", CultureInfo.InvariantCulture);
        var y = position.Y.ToString("F2", CultureInfo.InvariantCulture);
        try
        {
            CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"pointer\",\"x\":{x},\"y\":{y}}}");
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

    private static void OnEffectOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsCloudsBackground)dependencyObject).QueueSettingsPush();
    }

    private static void OnPageOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsCloudsBackground)dependencyObject).NavigateToCloudsPage();
    }

    private void QueueSettingsPush()
    {
        _settingsUpdateTimer.Stop();
        _settingsUpdateTimer.Start();
    }

    private async void OnSettingsUpdateTimerTick(object? sender, EventArgs e)
    {
        _settingsUpdateTimer.Stop();
        await PushSettingsAsync();
    }

    private async Task PushSettingsAsync()
    {
        if (CoreWebView2 is null || !_isVantaReady) return;
        static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        var cloudColor = ToHex(PrimaryColor);
        var skyColor = ToHex(SkyColor);
        var cloudShadowColor = ToHex(CloudShadowColor);
        var sunColor = ToHex(SunColor);
        var sunGlareColor = ToHex(SunGlareColor);
        var sunlightColor = ToHex(SunlightColor);
        var speed = AnimationSpeed.ToString("F3", CultureInfo.InvariantCulture);
        var enabled = IsAnimationEnabled ? "true" : "false";
        // CLOUDS 是全屏片元着色器。最大化时将 Vanta 默认 scale=3 提高到 5，
        // 卡片预览使用 6；只减少内部着色像素，颜色、速度等效果默认值不变。
        var renderScale = IsPreview ? 6 : IsFullScreen ? 5 : 3;
        try
        {
            await CoreWebView2.ExecuteScriptAsync(
                $"window.__cheemsUpdate?.({{skyColor:'{skyColor}',cloudColor:'{cloudColor}'," +
                $"cloudShadowColor:'{cloudShadowColor}',sunColor:'{sunColor}',sunGlareColor:'{sunGlareColor}'," +
                $"sunlightColor:'{sunlightColor}',speed:{speed},enabled:{enabled},renderScale:{renderScale}}});");
        }
        catch (InvalidOperationException)
        {
        }
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

        if (!IsVisible)
        {
            CoreWebView2.NavigateToString("<!doctype html><title>Background paused</title>");
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
            await PushSettingsAsync();
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }
}
