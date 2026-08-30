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
/// 使用本地 three.js r134 与 Vanta CELLS 原版脚本的离线背景层。
/// </summary>
public sealed class CheemsCellsBackground : WebView2CompositionControl, IStaticPreviewWebView
{
    private const string VirtualHostName = "cells.cheemsui.local";
    private Task? _initializationTask;
    private bool _isLoaded;
    private bool _isVantaReady;
    private bool _isReleased;
    private long _lastPointerMessageTimestamp;

    /// <summary>用户点击 CELLS 卡片时触发；页面可将当前效果应用到整个软件窗口。</summary>
    public event EventHandler? ApplyRequested;
    public event EventHandler? PreviewFrozen;

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsCellsBackground),
        new PropertyMetadata(false, OnPageOptionChanged));

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsCellsBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register(
        nameof(PrimaryColor), typeof(Color), typeof(CheemsCellsBackground),
        new PropertyMetadata(Color.FromRgb(0x8B, 0x5C, 0xF6), OnEffectOptionChanged));

    public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register(
        nameof(AnimationSpeed), typeof(double), typeof(CheemsCellsBackground),
        new PropertyMetadata(1d, OnEffectOptionChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(CheemsCellsBackground),
        new PropertyMetadata(true, OnEffectOptionChanged));

    public CheemsCellsBackground()
    {
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

    public bool IsPreview
    {
        get => (bool)GetValue(IsPreviewProperty);
        set => SetValue(IsPreviewProperty, value);
    }

    public Color PrimaryColor { get => (Color)GetValue(PrimaryColorProperty); set => SetValue(PrimaryColorProperty, value); }

    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }

    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    private Uri CellsPage => new($"https://{VirtualHostName}/cells.offline.html{(IsPreview ? "?preview=1" : string.Empty)}");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        try
        {
            if (IsVisible)
            {
                _initializationTask ??= InitializeAsync();
                await _initializationTask;
                NavigateToCellsPage();
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
                NavigateToCellsPage();
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

        var assetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "VantaCells");
        if (!File.Exists(Path.Combine(assetFolder, "cells.offline.html")))
        {
            throw new FileNotFoundException("未找到离线 Vanta CELLS 资源。", assetFolder);
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

    /// <summary>将宿主窗口的鼠标坐标转发给本地 Vanta 页面。</summary>
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
        ((CheemsCellsBackground)dependencyObject).UpdateClip();
    }

    private static void OnPageOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsCellsBackground)dependencyObject).NavigateToCellsPage();
    }

    private static void OnEffectOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        _ = ((CheemsCellsBackground)dependencyObject).PushSettingsAsync();
    }

    private async Task PushSettingsAsync()
    {
        if (_isReleased || !_isVantaReady) return;
        CoreWebView2? coreWebView;
        try { coreWebView = CoreWebView2; }
        catch (ObjectDisposedException) { return; }
        if (coreWebView is null) return;
        var color = $"#{PrimaryColor.R:X2}{PrimaryColor.G:X2}{PrimaryColor.B:X2}";
        var speed = AnimationSpeed.ToString("F3", CultureInfo.InvariantCulture);
        var enabled = IsAnimationEnabled ? "true" : "false";
        try
        {
            await coreWebView.ExecuteScriptAsync($"window.__cheemsUpdate?.({{color:'{color}',speed:{speed},enabled:{enabled}}});");
        }
        catch (InvalidOperationException)
        {
        }
    }

    void IStaticPreviewWebView.ReleasePreview()
    {
        _isReleased = true;
        _isVantaReady = false;
        Dispose();
    }

    private void NavigateToCellsPage()
    {
        _isVantaReady = false;
        if (_isLoaded && CoreWebView2 is not null && CoreWebView2.Source != CellsPage.AbsoluteUri)
        {
            CoreWebView2.Navigate(CellsPage.AbsoluteUri);
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
        if (CoreWebView2.Source != CellsPage.AbsoluteUri)
        {
            return;
        }

        if (!e.IsSuccess)
        {
            ErrorLog.Write(new InvalidOperationException($"离线 Vanta CELLS 页面导航失败：{e.WebErrorStatus}"));
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
                "Boolean(window.VANTA && window.VANTA.CELLS && document.querySelector('#cells-background canvas') && window.__cheemsSetPointer)");
            if (!string.Equals(result, "true", StringComparison.Ordinal))
            {
                ErrorLog.Write(new InvalidOperationException("离线 Vanta CELLS 脚本未创建渲染画布。"));
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
