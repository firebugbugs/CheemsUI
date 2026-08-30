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

/// <summary>使用本地 three.js r134 与 Vanta DOTS 原版脚本的离线背景层。</summary>
public sealed class CheemsDotsBackground : WebView2CompositionControl, IStaticPreviewWebView
{
    private const string VirtualHostName = "dots.cheemsui.local";
    private readonly DispatcherTimer _settingsUpdateTimer;
    private Task? _initializationTask;
    private bool _isLoaded;
    private bool _isVantaReady;
    private bool _isReleased;
    private long _lastPointerMessageTimestamp;

    public event EventHandler? ApplyRequested;
    public event EventHandler? PreviewFrozen;

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsDotsBackground),
        new PropertyMetadata(false, OnPageOptionChanged));
    public static readonly DependencyProperty IsFullScreenProperty = DependencyProperty.Register(
        nameof(IsFullScreen), typeof(bool), typeof(CheemsDotsBackground),
        new PropertyMetadata(false, OnEffectOptionChanged));
    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsDotsBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));
    public static readonly DependencyProperty PrimaryColorProperty = RegisterEffectOption(
        nameof(PrimaryColor), Color.FromRgb(0xFF, 0x88, 0x20));
    public static readonly DependencyProperty SecondaryColorProperty = RegisterEffectOption(
        nameof(SecondaryColor), Color.FromRgb(0xFF, 0x88, 0x20));
    public static readonly DependencyProperty BackgroundColorProperty = RegisterEffectOption(
        nameof(BackgroundColor), Color.FromRgb(0x22, 0x22, 0x22));
    public static readonly DependencyProperty DotSizeProperty = RegisterEffectOption(nameof(DotSize), 3d);
    public static readonly DependencyProperty SpacingProperty = RegisterEffectOption(nameof(Spacing), 35d);
    public static readonly DependencyProperty ShowLinesProperty = RegisterEffectOption(nameof(ShowLines), true);
    public static readonly DependencyProperty AnimationSpeedProperty = RegisterEffectOption(nameof(AnimationSpeed), 1d);
    public static readonly DependencyProperty IsAnimationEnabledProperty = RegisterEffectOption(nameof(IsAnimationEnabled), true);

    public CheemsDotsBackground()
    {
        _settingsUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _settingsUpdateTimer.Tick += OnSettingsUpdateTimerTick;
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
        SizeChanged += (_, _) => UpdateClip();
    }

    public bool IsPreview { get => (bool)GetValue(IsPreviewProperty); set => SetValue(IsPreviewProperty, value); }
    public bool IsFullScreen { get => (bool)GetValue(IsFullScreenProperty); set => SetValue(IsFullScreenProperty, value); }
    public double ClipRadius { get => (double)GetValue(ClipRadiusProperty); set => SetValue(ClipRadiusProperty, value); }
    public Color PrimaryColor { get => (Color)GetValue(PrimaryColorProperty); set => SetValue(PrimaryColorProperty, value); }
    public Color SecondaryColor { get => (Color)GetValue(SecondaryColorProperty); set => SetValue(SecondaryColorProperty, value); }
    public Color BackgroundColor { get => (Color)GetValue(BackgroundColorProperty); set => SetValue(BackgroundColorProperty, value); }
    public double DotSize { get => (double)GetValue(DotSizeProperty); set => SetValue(DotSizeProperty, value); }
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public bool ShowLines { get => (bool)GetValue(ShowLinesProperty); set => SetValue(ShowLinesProperty, value); }
    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }
    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    private static DependencyProperty RegisterEffectOption<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(CheemsDotsBackground),
            new PropertyMetadata(defaultValue, OnEffectOptionChanged));

    private Uri DotsPage => new($"https://{VirtualHostName}/dots.offline.html{(IsPreview ? "?preview=1" : string.Empty)}");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        try
        {
            if (IsVisible)
            {
                _initializationTask ??= InitializeAsync();
                await _initializationTask;
                NavigateToDotsPage();
            }
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
                NavigateToDotsPage();
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
        var assetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "VantaDots");
        if (!File.Exists(Path.Combine(assetFolder, "dots.offline.html")))
        {
            throw new FileNotFoundException("未找到离线 Vanta DOTS 资源。", assetFolder);
        }

        CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName, assetFolder, CoreWebView2HostResourceAccessKind.DenyCors);
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

    public void SetPointerPosition(Point position)
    {
        if (CoreWebView2 is null || !_isVantaReady) return;
        var now = Stopwatch.GetTimestamp();
        if ((now - _lastPointerMessageTimestamp) / (double)Stopwatch.Frequency < 1d / 60) return;
        _lastPointerMessageTimestamp = now;

        var x = position.X.ToString("F2", CultureInfo.InvariantCulture);
        var y = position.Y.ToString("F2", CultureInfo.InvariantCulture);
        try
        {
            CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"pointer\",\"x\":{x},\"y\":{y}}}");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void OnClipRadiusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((CheemsDotsBackground)dependencyObject).UpdateClip();

    private static void OnEffectOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((CheemsDotsBackground)dependencyObject).QueueSettingsPush();

    private static void OnPageOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((CheemsDotsBackground)dependencyObject).NavigateToDotsPage();

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
        static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        var speed = AnimationSpeed.ToString("F3", CultureInfo.InvariantCulture);
        var size = DotSize.ToString("F2", CultureInfo.InvariantCulture);
        var spacing = Spacing.ToString("F2", CultureInfo.InvariantCulture);
        var enabled = IsAnimationEnabled ? "true" : "false";
        var showLines = ShowLines ? "true" : "false";
        var renderScale = IsPreview ? 2 : IsFullScreen ? 1.5 : 1;
        try
        {
            await coreWebView.ExecuteScriptAsync(
                $"window.__cheemsUpdate?.({{primaryColor:'{ToHex(PrimaryColor)}',secondaryColor:'{ToHex(SecondaryColor)}'," +
                $"backgroundColor:'{ToHex(BackgroundColor)}',size:{size},spacing:{spacing},showLines:{showLines}," +
                $"speed:{speed},enabled:{enabled},renderScale:{renderScale.ToString("F1", CultureInfo.InvariantCulture)}}});");
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

    private void NavigateToDotsPage()
    {
        _isVantaReady = false;
        if (_isLoaded && CoreWebView2 is not null && CoreWebView2.Source != DotsPage.AbsoluteUri)
        {
            CoreWebView2.Navigate(DotsPage.AbsoluteUri);
        }
    }

    private void UpdateClip()
    {
        Clip = ClipRadius <= 0 || ActualWidth <= 0 || ActualHeight <= 0
            ? null
            : new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius);
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (CoreWebView2.Source != DotsPage.AbsoluteUri) return;
        if (!e.IsSuccess)
        {
            ErrorLog.Write(new InvalidOperationException($"离线 Vanta DOTS 页面导航失败：{e.WebErrorStatus}"));
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
                "Boolean(window.VANTA && window.VANTA.DOTS && document.querySelector('#dots-background canvas') && window.__cheemsSetPointer)");
            if (!string.Equals(result, "true", StringComparison.Ordinal))
            {
                ErrorLog.Write(new InvalidOperationException("离线 Vanta DOTS 脚本未创建渲染画布。"));
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
