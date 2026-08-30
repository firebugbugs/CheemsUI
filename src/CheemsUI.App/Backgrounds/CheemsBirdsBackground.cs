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
/// 使用本地 three.js r134 与 Vanta BIRDS 原版脚本的离线背景层。
/// </summary>
public sealed class CheemsBirdsBackground : WebView2CompositionControl, IStaticPreviewWebView
{
    private const string VirtualHostName = "birds.cheemsui.local";
    private Task? _initializationTask;
    private bool _isLoaded;
    private bool _isVantaReady;
    private bool _isReleased;
    private long _lastPointerMessageTimestamp;
    private readonly DispatcherTimer _settingsUpdateTimer;

    /// <summary>
    /// 用户点击鸟群卡片时触发；页面可将当前效果应用到整个软件窗口。
    /// </summary>
    public event EventHandler? ApplyRequested;
    public event EventHandler? PreviewFrozen;

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsBirdsBackground),
        new PropertyMetadata(false, OnPageOptionChanged));

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsBirdsBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register(
        nameof(PrimaryColor), typeof(Color), typeof(CheemsBirdsBackground),
        new PropertyMetadata(Color.FromRgb(0x8B, 0x5C, 0xF6), OnEffectOptionChanged));

    public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register(
        nameof(AnimationSpeed), typeof(double), typeof(CheemsBirdsBackground),
        new PropertyMetadata(1d, OnEffectOptionChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(CheemsBirdsBackground),
        new PropertyMetadata(true, OnEffectOptionChanged));

    public static readonly DependencyProperty BackgroundColorProperty = RegisterEffectOption<Color>(nameof(BackgroundColor), Color.FromRgb(0x07, 0x19, 0x0F));
    public static readonly DependencyProperty BackgroundAlphaProperty = RegisterEffectOption<double>(nameof(BackgroundAlpha), 1d);
    public static readonly DependencyProperty SecondaryColorProperty = RegisterEffectOption<Color>(nameof(SecondaryColor), Color.FromRgb(0x8B, 0x5C, 0xF6));
    public static readonly DependencyProperty BirdSizeProperty = RegisterEffectOption<double>(nameof(BirdSize), 1d);
    public static readonly DependencyProperty WingSpanProperty = RegisterEffectOption<double>(nameof(WingSpan), 30d);
    public static readonly DependencyProperty SpeedLimitProperty = RegisterEffectOption<double>(nameof(SpeedLimit), 5d);
    public static readonly DependencyProperty SeparationProperty = RegisterEffectOption<double>(nameof(Separation), 20d);
    public static readonly DependencyProperty AlignmentProperty = RegisterEffectOption<double>(nameof(Alignment), 20d);
    public static readonly DependencyProperty CohesionProperty = RegisterEffectOption<double>(nameof(Cohesion), 20d);
    public static readonly DependencyProperty QuantityProperty = RegisterEffectOption<int>(nameof(Quantity), 5);

    public CheemsBirdsBackground()
    {
        // 拖动滑块会连续触发属性变更。合并为一次脚本调用，避免反复重建 Vanta 的 GPU 纹理。
        _settingsUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _settingsUpdateTimer.Tick += OnSettingsUpdateTimerTick;
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
        SizeChanged += (_, _) => UpdateClip();
    }

    /// <summary>以较低鸟群数量渲染卡片预览，避免与全窗口背景竞争 GPU。</summary>
    public bool IsPreview
    {
        get => (bool)GetValue(IsPreviewProperty);
        set => SetValue(IsPreviewProperty, value);
    }

    /// <summary>当前 WebGL 表面的统一裁剪圆角。</summary>
    public double ClipRadius
    {
        get => (double)GetValue(ClipRadiusProperty);
        set => SetValue(ClipRadiusProperty, value);
    }

    public Color PrimaryColor { get => (Color)GetValue(PrimaryColorProperty); set => SetValue(PrimaryColorProperty, value); }

    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }

    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    public Color BackgroundColor { get => (Color)GetValue(BackgroundColorProperty); set => SetValue(BackgroundColorProperty, value); }
    public double BackgroundAlpha { get => (double)GetValue(BackgroundAlphaProperty); set => SetValue(BackgroundAlphaProperty, value); }
    public Color SecondaryColor { get => (Color)GetValue(SecondaryColorProperty); set => SetValue(SecondaryColorProperty, value); }
    public double BirdSize { get => (double)GetValue(BirdSizeProperty); set => SetValue(BirdSizeProperty, value); }
    public double WingSpan { get => (double)GetValue(WingSpanProperty); set => SetValue(WingSpanProperty, value); }
    public double SpeedLimit { get => (double)GetValue(SpeedLimitProperty); set => SetValue(SpeedLimitProperty, value); }
    public double Separation { get => (double)GetValue(SeparationProperty); set => SetValue(SeparationProperty, value); }
    public double Alignment { get => (double)GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }
    public double Cohesion { get => (double)GetValue(CohesionProperty); set => SetValue(CohesionProperty, value); }
    public int Quantity { get => (int)GetValue(QuantityProperty); set => SetValue(QuantityProperty, value); }

    private static DependencyProperty RegisterEffectOption<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(CheemsBirdsBackground), new PropertyMetadata(defaultValue, OnEffectOptionChanged));

    private Uri BirdsPage => new($"https://{VirtualHostName}/birds.offline.html{(IsPreview ? "?preview=1" : string.Empty)}");

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _isLoaded = true;

        try
        {
            if (IsVisible)
            {
                _initializationTask ??= InitializeAsync();
                await _initializationTask;
                NavigateToBirdsPage();
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
        _settingsUpdateTimer.Stop();

        // Vanta 的 requestAnimationFrame 在空白页中不存在，避免页面离开后继续渲染。
        if (!IsPreview)
        {
            CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
        }
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && _isLoaded)
        {
            try
            {
                _initializationTask ??= InitializeAsync();
                await _initializationTask;
                NavigateToBirdsPage();
            }
            catch (Exception exception)
            {
                ErrorLog.Write(exception);
            }
        }
        else
        {
            _isVantaReady = false;
            if (!IsPreview)
            {
                CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
            }
        }
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
        switch (e.TryGetWebMessageAsString())
        {
            case "apply-background":
                ApplyRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "preview-frozen":
                PreviewFrozen?.Invoke(this, EventArgs.Empty);
                break;
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
            // 比逐帧 ExecuteScriptAsync 更接近浏览器原生事件通道，不会因脚本任务排队而滞后。
            CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"pointer\",\"x\":{x},\"y\":{y}}}");
        }
        catch (InvalidOperationException)
        {
            // 页面切换时可能恰好失去 WebView；下一次 MouseMove 会重新发送。
        }
    }

    private static void OnPageOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsBirdsBackground)dependencyObject).NavigateToBirdsPage();
    }

    private static void OnClipRadiusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsBirdsBackground)dependencyObject).UpdateClip();
    }

    private static void OnEffectOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsBirdsBackground)dependencyObject).QueueSettingsPush();
    }

    private void QueueSettingsPush()
    {
        if (_isReleased) return;
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
        if (_isReleased || !_isVantaReady) return;
        CoreWebView2? coreWebView;
        try { coreWebView = CoreWebView2; }
        catch (ObjectDisposedException) { return; }
        if (coreWebView is null) return;
        var primaryColor = $"#{PrimaryColor.R:X2}{PrimaryColor.G:X2}{PrimaryColor.B:X2}";
        var secondaryColor = $"#{SecondaryColor.R:X2}{SecondaryColor.G:X2}{SecondaryColor.B:X2}";
        var backgroundColor = $"#{BackgroundColor.R:X2}{BackgroundColor.G:X2}{BackgroundColor.B:X2}";
        var speed = AnimationSpeed.ToString("F3", CultureInfo.InvariantCulture);
        var backgroundAlpha = BackgroundAlpha.ToString("F3", CultureInfo.InvariantCulture);
        var birdSize = BirdSize.ToString("F3", CultureInfo.InvariantCulture);
        var wingSpan = WingSpan.ToString("F3", CultureInfo.InvariantCulture);
        var speedLimit = SpeedLimit.ToString("F3", CultureInfo.InvariantCulture);
        var separation = Separation.ToString("F3", CultureInfo.InvariantCulture);
        var alignment = Alignment.ToString("F3", CultureInfo.InvariantCulture);
        var cohesion = Cohesion.ToString("F3", CultureInfo.InvariantCulture);
        var enabled = IsAnimationEnabled ? "true" : "false";
        // 预览历来固定少一档鸟群以减轻 GPU 负担；默认 Quantity=5 时保留该既有行为。
        var effectiveQuantity = IsPreview && Quantity == 5 ? 4 : Quantity;
        try
        {
            await coreWebView.ExecuteScriptAsync($"window.__cheemsUpdate?.({{primaryColor:'{primaryColor}',secondaryColor:'{secondaryColor}',backgroundColor:'{backgroundColor}',backgroundAlpha:{backgroundAlpha},birdSize:{birdSize},wingSpan:{wingSpan},speedLimit:{speedLimit},separation:{separation},alignment:{alignment},cohesion:{cohesion},quantity:{effectiveQuantity},speed:{speed},enabled:{enabled}}});");
        }
        catch (InvalidOperationException)
        {
        }
    }

    void IStaticPreviewWebView.ReleasePreview()
    {
        _isReleased = true;
        _isVantaReady = false;
        _settingsUpdateTimer.Stop();
        Dispose();
    }

    private void NavigateToBirdsPage()
    {
        _isVantaReady = false;
        if (_isLoaded && CoreWebView2 is not null && CoreWebView2.Source != BirdsPage.AbsoluteUri)
        {
            CoreWebView2.Navigate(BirdsPage.AbsoluteUri);
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
        if (CoreWebView2.Source != BirdsPage.AbsoluteUri)
        {
            return;
        }

        if (!e.IsSuccess)
        {
            ErrorLog.Write(new InvalidOperationException($"离线 Vanta 页面导航失败：{e.WebErrorStatus}"));
            return;
        }

        if (!IsVisible && !IsPreview)
        {
            CoreWebView2.NavigateToString("<!doctype html><title>Background paused</title>");
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
            await PushSettingsAsync();
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }
}
